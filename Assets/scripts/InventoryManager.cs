using System.Collections.Generic;
using UnityEngine;
using ReactorBreach.InventorySystem;

public class InventoryManager : MonoBehaviour, ISaveable
{
    public static InventoryManager Instance;

    [SerializeField] private string uniqueId = "InventoryManager";

    [Header("Item Definitions")]
    public List<ItemDef> itemDefinitions = new List<ItemDef>();

    private Dictionary<string, int> resources = new Dictionary<string, int>();
    private Dictionary<string, ItemDef> itemLookup = new Dictionary<string, ItemDef>();
    private Dictionary<string, ItemSO> itemSOById = new Dictionary<string, ItemSO>();
    private static bool syncing;

    public System.Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (var def in itemDefinitions)
        {
            itemLookup[def.id] = def;
            resources[def.id] = def.startAmount;
            if (def.itemSO != null)
                itemSOById[def.id] = def.itemSO;
        }
    }

    private void Start()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.Register(this);

        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += SyncFromNewInventory;
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= SyncFromNewInventory;
    }

    // ===== SYNC (novo <-> antigo) =====

    private void SyncFromNewInventory()
    {
        if (syncing || Inventory.Instance == null) return;

        syncing = true;
        foreach (var pair in itemSOById)
        {
            resources[pair.Key] = Inventory.Instance.CountItem(pair.Value);
        }
        syncing = false;

        OnInventoryChanged?.Invoke();
    }

    private void PushToNewInventory(string id)
    {
        if (syncing || Inventory.Instance == null) return;
        if (!itemSOById.TryGetValue(id, out ItemSO itemSO) || itemSO == null) return;

        int target = resources.ContainsKey(id) ? resources[id] : 0;

        syncing = true;
        int current = Inventory.Instance.CountItem(itemSO);
        if (target > current)
            Inventory.Instance.AddItem(itemSO, target - current);
        else if (target < current)
            Inventory.Instance.RemoveItem(itemSO, current - target);
        syncing = false;
    }

    // ===== CORE =====

    public bool AddResource(string resourceName, int amount)
    {
        if (amount <= 0) return false;

        if (!resources.ContainsKey(resourceName))
        {
            resources[resourceName] = 0;
        }

        int max = GetMaxStack(resourceName);
        int space = max - resources[resourceName];
        int toAdd = Mathf.Min(amount, space);
        if (toAdd <= 0) return false;

        resources[resourceName] += toAdd;
        OnInventoryChanged?.Invoke();
        PushToNewInventory(resourceName);
        return true;
    }

    public bool RemoveResource(string resourceName, int amount)
    {
        if (!resources.ContainsKey(resourceName))
            return false;
        if (resources[resourceName] < amount)
            return false;

        resources[resourceName] -= amount;
        OnInventoryChanged?.Invoke();
        PushToNewInventory(resourceName);
        return true;
    }

    public bool HasResource(string resourceName, int amount)
    {
        return resources.ContainsKey(resourceName) && resources[resourceName] >= amount;
    }

    public int GetResourceAmount(string resourceName)
    {
        if (!resources.ContainsKey(resourceName))
            return 0;
        return resources[resourceName];
    }

    public int GetMaxStack(string resourceName)
    {
        if (itemLookup.ContainsKey(resourceName))
            return itemLookup[resourceName].maxStack;
        return 100;
    }

    public ItemDef GetItemDef(string resourceName)
    {
        if (itemLookup.ContainsKey(resourceName))
            return itemLookup[resourceName];
        return null;
    }

    public Dictionary<string, int> GetAll() => resources;

    public void Clear()
    {
        resources.Clear();
        OnInventoryChanged?.Invoke();

        if (syncing || Inventory.Instance == null) return;
        syncing = true;
        foreach (var pair in itemSOById)
        {
            int current = Inventory.Instance.CountItem(pair.Value);
            if (current > 0)
                Inventory.Instance.RemoveItem(pair.Value, current);
        }
        syncing = false;
    }

    // ===== SAVE =====

    public string GetUniqueId() => uniqueId;

    public string CaptureState()
    {
        InventorySaveData data = new InventorySaveData();
        foreach (var r in resources)
            data.items.Add(new InventoryItemSave { id = r.Key, amount = r.Value });
        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
        if (data == null) return;

        resources.Clear();
        foreach (var item in data.items)
            resources[item.id] = item.amount;

        OnInventoryChanged?.Invoke();

        if (syncing || Inventory.Instance == null) return;
        syncing = true;
        foreach (var pair in itemSOById)
        {
            int target = resources.ContainsKey(pair.Key) ? resources[pair.Key] : 0;
            int current = Inventory.Instance.CountItem(pair.Value);
            if (target > current)
                Inventory.Instance.AddItem(pair.Value, target - current);
            else if (target < current)
                Inventory.Instance.RemoveItem(pair.Value, current - target);
        }
        syncing = false;
    }
}

[System.Serializable]
public class ItemDef
{
    public string id;
    public string displayName;
    public Color color = Color.white;
    public Sprite icon;
    public ItemCategory category;
    public int maxStack = 100;
    public int startAmount = 0;
    public ItemSO itemSO;
}

public enum ItemCategory
{
    Ore,
    Ingot,
    Component,
    Fuel,
    Currency,
    Other
}

[System.Serializable]
public class InventorySaveData
{
    public List<InventoryItemSave> items = new List<InventoryItemSave>();
}

[System.Serializable]
public class InventoryItemSave
{
    public string id;
    public int amount;
}
