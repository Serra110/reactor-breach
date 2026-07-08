using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour, ISaveable
{
    public static InventoryManager Instance;

    [SerializeField] private string uniqueId = "Inventory";

    private Dictionary<string, int> resources = new Dictionary<string, int>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("InventoryManager ativo");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        resources["Metal"] = 0;
        resources["Uranium"] = 0;
        resources["Money"] = 0;
    }


    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Register(this);
            Debug.Log("Inventory registado no SaveManager");
        }
    }



    public void AddResource(string resourceName, int amount)
    {
        if (!resources.ContainsKey(resourceName))
            resources[resourceName] = 0;


        resources[resourceName] += amount;

        Debug.Log(resourceName + ": " + resources[resourceName]);
    }



    public bool RemoveResource(string resourceName, int amount)
    {
        if (!resources.ContainsKey(resourceName))
            return false;


        if (resources[resourceName] < amount)
            return false;


        resources[resourceName] -= amount;

        return true;
    }



    public bool HasResource(string resourceName, int amount)
    {
        return resources.ContainsKey(resourceName) &&
               resources[resourceName] >= amount;
    }



    public int GetResourceAmount(string resourceName)
    {
        if (!resources.ContainsKey(resourceName))
            return 0;

        return resources[resourceName];
    }




    // ==========================
    // SAVE SYSTEM
    // ==========================


    public string GetUniqueId()
    {
        return uniqueId;
    }



    public string CaptureState()
    {
        InventorySaveData data = new InventorySaveData();


        foreach (var resource in resources)
        {
            data.items.Add(new InventoryItemSave
            {
                id = resource.Key,
                amount = resource.Value
            });
        }


        string json = JsonUtility.ToJson(data);

        Debug.Log("Inventory Saved: " + json);

        return json;
    }



    public void RestoreState(string json)
    {
        InventorySaveData data =
            JsonUtility.FromJson<InventorySaveData>(json);


        if (data == null)
        {
            Debug.LogWarning("Inventory save vazio");
            return;
        }


        resources.Clear();


        foreach (var item in data.items)
        {
            resources[item.id] = item.amount;

            Debug.Log(
                "Loaded " + item.id + ": " + item.amount
            );
        }


        Debug.Log("Inventory Loaded!");
    }
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