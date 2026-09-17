using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReactorBreach.InventorySystem
{
    [System.Serializable]
    public class InventoryEntry
    {
        public ItemSO item;
        public int amount = 1;
    }

    public class Inventory : MonoBehaviour, ISaveable
    {
        public static Inventory Instance;

        [Header("Config")]
        public int size = 6;

        [Header("References")]
        public ItemSO IronIngot;
        public GameObject hotbarObj;
        public GameObject inventorySlotParent;
        public GameObject Container;
        public Transform handSlot;
        public Transform dragLayer;

        [Header("Save")]
        [Tooltip("Itens conhecidos para restaurar o inventário a partir do nome gravado.")]
        public List<ItemSO> knownItems = new List<ItemSO>();

        [Header("Drop")]
        public Transform dropPoint;
        public float dropForce = 2.5f;

        [Header("Initial Items")]
        [SerializeField]
        private List<InventoryEntry> initialItems = new List<InventoryEntry>();

        // Inventário principal (painel)
        private ItemSO[] dataItems;
        private int[] dataAmounts;

        // Hotbar independente (já não espelha o painel)
        private ItemSO[] hotbarItems;
        private int[] hotbarAmounts;

        private List<Slot> panelSlots = new List<Slot>();
        private List<Slot> hotbarSlots = new List<Slot>();
        private int panelCount;
        private int hotbarCount;

        private readonly string uniqueId = "Inventory";
        private Dictionary<string, ItemSO> itemLookupByName;

        public event Action OnInventoryChanged;
        public event Action OnInventoryFull;

        private int selectedHotbar = 0;
        private GameObject currentHandModel;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            CollectSlots(inventorySlotParent, panelSlots);
            CollectSlots(hotbarObj, hotbarSlots);

            panelCount = panelSlots.Count > 0 ? panelSlots.Count : size;
            dataItems = new ItemSO[panelCount];
            dataAmounts = new int[panelCount];

            hotbarCount = hotbarSlots.Count;
            hotbarItems = new ItemSO[hotbarCount];
            hotbarAmounts = new int[hotbarCount];

            for (int i = 0; i < panelSlots.Count; i++)
                panelSlots[i].Init(i, this, false);
            for (int i = 0; i < hotbarSlots.Count; i++)
                hotbarSlots[i].Init(i, this, true);

            itemLookupByName = new Dictionary<string, ItemSO>();
            if (IronIngot != null && !itemLookupByName.ContainsKey(IronIngot.itemName))
                itemLookupByName[IronIngot.itemName] = IronIngot;
            if (initialItems != null)
                foreach (InventoryEntry entry in initialItems)
                    if (entry.item != null && !itemLookupByName.ContainsKey(entry.item.itemName))
                        itemLookupByName[entry.item.itemName] = entry.item;
            foreach (ItemSO known in knownItems)
                if (known != null && !itemLookupByName.ContainsKey(known.itemName))
                    itemLookupByName[known.itemName] = known;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            PopulateInitialItems();
            RefreshAllSlotUIs();
            SelectHotbar(0);

            if (SaveManager.Instance != null)
                SaveManager.Instance.Register(this);
        }

        private void CollectSlots(GameObject parent, List<Slot> target)
        {
            if (parent == null) return;
            Slot[] found = parent.GetComponentsInChildren<Slot>(true);
            foreach (var slot in found)
                target.Add(slot);
        }

        private void PopulateInitialItems()
        {
            if (initialItems == null || initialItems.Count == 0) return;
            foreach (InventoryEntry entry in initialItems)
            {
                if (entry == null || entry.item == null) continue;
                AddItem(entry.item, entry.amount);
            }
        }

        // ===== UPDATE =====

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (Container != null)
                {
                    Container.SetActive(!Container.activeInHierarchy);

                    if (Container.activeInHierarchy)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                        RefreshAllSlotUIs();
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Q))
                DropSelectedItem();

            HandleHotbarInput();
        }

        // ===== ADD / REMOVE (inventário principal) =====

        public int AddItem(ItemSO item, int amount)
        {
            if (item == null || amount <= 0) return amount;

            int remaining = amount;

            for (int i = 0; i < panelCount && remaining > 0; i++)
            {
                if (dataItems[i] == item && dataAmounts[i] > 0 && dataAmounts[i] < item.maxStackSize)
                {
                    int space = item.maxStackSize - dataAmounts[i];
                    int toAdd = Mathf.Min(space, remaining);
                    dataAmounts[i] += toAdd;
                    remaining -= toAdd;
                }
            }

            for (int i = 0; i < hotbarCount && remaining > 0; i++)
            {
                if (hotbarItems[i] == item && hotbarAmounts[i] > 0 && hotbarAmounts[i] < item.maxStackSize)
                {
                    int space = item.maxStackSize - hotbarAmounts[i];
                    int toAdd = Mathf.Min(space, remaining);
                    hotbarAmounts[i] += toAdd;
                    remaining -= toAdd;
                }
            }

            for (int i = 0; i < panelCount && remaining > 0; i++)
            {
                if (dataItems[i] == null)
                {
                    int toAdd = Mathf.Min(item.maxStackSize, remaining);
                    dataItems[i] = item;
                    dataAmounts[i] = toAdd;
                    remaining -= toAdd;
                }
            }

            for (int i = 0; i < hotbarCount && remaining > 0; i++)
            {
                if (hotbarItems[i] == null)
                {
                    int toAdd = Mathf.Min(item.maxStackSize, remaining);
                    hotbarItems[i] = item;
                    hotbarAmounts[i] = toAdd;
                    remaining -= toAdd;
                }
            }

            int added = amount - remaining;
            if (added > 0)
            {
                OnInventoryChanged?.Invoke();
                RefreshAllSlotUIs();
                UpdateHandItem();
            }
            if (remaining > 0)
                OnInventoryFull?.Invoke();

            return remaining;
        }

        public bool RemoveItem(ItemSO item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            int remaining = amount;

            for (int i = 0; i < panelCount && remaining > 0; i++)
            {
                if (dataItems[i] == item && dataAmounts[i] > 0)
                {
                    int take = Mathf.Min(remaining, dataAmounts[i]);
                    dataAmounts[i] -= take;
                    if (dataAmounts[i] <= 0)
                    {
                        dataItems[i] = null;
                        dataAmounts[i] = 0;
                    }
                    remaining -= take;
                }
            }

            for (int i = 0; i < hotbarCount && remaining > 0; i++)
            {
                if (hotbarItems[i] == item && hotbarAmounts[i] > 0)
                {
                    int take = Mathf.Min(remaining, hotbarAmounts[i]);
                    hotbarAmounts[i] -= take;
                    if (hotbarAmounts[i] <= 0)
                    {
                        hotbarItems[i] = null;
                        hotbarAmounts[i] = 0;
                    }
                    remaining -= take;
                }
            }

            if (amount - remaining > 0)
            {
                OnInventoryChanged?.Invoke();
                RefreshAllSlotUIs();
                UpdateHandItem();
            }

            return remaining <= 0;
        }

        public bool HasItem(ItemSO item, int amount = 1)
        {
            return CountItem(item) >= amount;
        }

        public int CountItem(ItemSO item)
        {
            int total = 0;
            for (int i = 0; i < panelCount; i++)
                if (dataItems[i] == item)
                    total += dataAmounts[i];
            for (int i = 0; i < hotbarCount; i++)
                if (hotbarItems[i] == item)
                    total += hotbarAmounts[i];
            return total;
        }

        public bool IsFull()
        {
            for (int i = 0; i < panelCount; i++)
                if (dataItems[i] == null) return false;
            for (int i = 0; i < hotbarCount; i++)
                if (hotbarItems[i] == null) return false;
            return true;
        }

        public int GetSlotCount() => panelCount + hotbarCount;

        public void Clear()
        {
            for (int i = 0; i < panelCount; i++)
            {
                dataItems[i] = null;
                dataAmounts[i] = 0;
            }
            for (int i = 0; i < hotbarCount; i++)
            {
                hotbarItems[i] = null;
                hotbarAmounts[i] = 0;
            }

            OnInventoryChanged?.Invoke();
            RefreshAllSlotUIs();
            UpdateHandItem();
        }

        public bool TryGetItemAtIndex(int index, out ItemSO item, out int amount)
        {
            item = null;
            amount = 0;
            if (index < 0) return false;

            if (index < panelCount)
            {
                if (dataItems[index] == null) return false;
                item = dataItems[index];
                amount = dataAmounts[index];
                return true;
            }

            int hotbarIndex = index - panelCount;
            if (hotbarIndex >= hotbarCount) return false;
            if (hotbarItems[hotbarIndex] == null) return false;
            item = hotbarItems[hotbarIndex];
            amount = hotbarAmounts[hotbarIndex];
            return true;
        }

        // ===== MOVE / MERGE / SPLIT (com noção de container) =====

        private bool GetEntry(int index, bool hotbar, out ItemSO item, out int amount)
        {
            item = null;
            amount = 0;
            if (hotbar)
            {
                if (index < 0 || index >= hotbarCount) return false;
                item = hotbarItems[index];
                amount = hotbarAmounts[index];
            }
            else
            {
                if (index < 0 || index >= panelCount) return false;
                item = dataItems[index];
                amount = dataAmounts[index];
            }
            return true;
        }

        private void SetEntry(int index, bool hotbar, ItemSO item, int amount)
        {
            if (hotbar)
            {
                if (index < 0 || index >= hotbarCount) return;
                hotbarItems[index] = item;
                hotbarAmounts[index] = amount;
            }
            else
            {
                if (index < 0 || index >= panelCount) return;
                dataItems[index] = item;
                dataAmounts[index] = amount;
            }
        }

        public void MoveOrMergeSlot(int fromIndex, bool fromHotbar, int toIndex, bool toHotbar)
        {
            if (fromIndex == toIndex && fromHotbar == toHotbar) return;

            if (!GetEntry(fromIndex, fromHotbar, out ItemSO fromItem, out int fromAmount)) return;
            if (fromItem == null || fromAmount <= 0) return;

            GetEntry(toIndex, toHotbar, out ItemSO toItem, out int toAmount);

            if (toItem == null)
            {
                SetEntry(toIndex, toHotbar, fromItem, fromAmount);
                SetEntry(fromIndex, fromHotbar, null, 0);
            }
            else if (toItem == fromItem)
            {
                int max = fromItem.maxStackSize;
                int space = max - toAmount;
                int moved = Mathf.Min(space, fromAmount);
                SetEntry(toIndex, toHotbar, toItem, toAmount + moved);
                int rest = fromAmount - moved;
                if (rest <= 0)
                    SetEntry(fromIndex, fromHotbar, null, 0);
                else
                    SetEntry(fromIndex, fromHotbar, fromItem, rest);
            }
            else
            {
                SetEntry(toIndex, toHotbar, fromItem, fromAmount);
                SetEntry(fromIndex, fromHotbar, toItem, toAmount);
            }

            OnInventoryChanged?.Invoke();
            RefreshAllSlotUIs();
            UpdateHandItem();
        }

        public void MoveOrMergeSlot(int fromIndex, int toIndex)
        {
            MoveOrMergeSlot(fromIndex, false, toIndex, false);
        }

        public void SplitStack(int slotIndex, bool hotbar = false)
        {
            if (!GetEntry(slotIndex, hotbar, out ItemSO item, out int amount)) return;
            if (item == null || amount < 2) return;

            int emptyIndex = -1;
            for (int i = 0; i < (hotbar ? hotbarCount : panelCount); i++)
            {
                if (!GetEntry(i, hotbar, out ItemSO it, out _) || it == null)
                {
                    emptyIndex = i;
                    break;
                }
            }
            if (emptyIndex < 0) return;

            int half = amount / 2;
            SetEntry(emptyIndex, hotbar, item, half);
            SetEntry(slotIndex, hotbar, item, amount - half);

            OnInventoryChanged?.Invoke();
            RefreshAllSlotUIs();
            UpdateHandItem();
        }

        public void UseItem(int slotIndex, bool hotbar = false)
        {
            if (!GetEntry(slotIndex, hotbar, out ItemSO item, out int amount)) return;
            if (item == null || amount <= 0) return;

            int left = amount - 1;
            if (left <= 0)
                SetEntry(slotIndex, hotbar, null, 0);
            else
                SetEntry(slotIndex, hotbar, item, left);

            OnInventoryChanged?.Invoke();
            RefreshAllSlotUIs();
            UpdateHandItem();
        }

        // ===== HOTBAR =====

        private void HandleHotbarInput()
        {
            for (int i = 0; i < hotbarSlots.Count && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectHotbar(i);
                    break;
                }
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                int next = selectedHotbar - 1;
                if (next < 0) next = hotbarSlots.Count - 1;
                SelectHotbar(next);
            }
            else if (scroll < 0f)
            {
                int next = selectedHotbar + 1;
                if (next >= hotbarSlots.Count) next = 0;
                SelectHotbar(next);
            }
        }

        public void SelectHotbar(int index)
        {
            if (index < 0 || index >= hotbarCount) return;

            for (int i = 0; i < hotbarSlots.Count; i++)
                hotbarSlots[i].SetSelected(i == index);

            selectedHotbar = index;
            UpdateHandItem();
        }

        public int GetSelectedHotbarIndex() => selectedHotbar;

        public bool TryGetHotbarItem(int index, out ItemSO item, out int amount)
        {
            item = null;
            amount = 0;
            if (index < 0 || index >= hotbarCount) return false;
            if (hotbarItems[index] == null) return false;
            item = hotbarItems[index];
            amount = hotbarAmounts[index];
            return true;
        }

        // ===== HAND ITEM =====

        private void UpdateHandItem()
        {
            if (currentHandModel != null)
            {
                Destroy(currentHandModel);
                currentHandModel = null;
            }

            if (handSlot == null) return;
            if (selectedHotbar < 0 || selectedHotbar >= hotbarCount) return;
            if (hotbarItems[selectedHotbar] == null) return;

            ItemSO item = hotbarItems[selectedHotbar];
            if (item.handItemPrefab == null) return;

            currentHandModel = Instantiate(item.handItemPrefab, handSlot);
            currentHandModel.transform.localPosition = Vector3.zero;
            currentHandModel.transform.localRotation = Quaternion.identity;
        }

        // ===== DROP =====

        public bool DropItem(int slotIndex, int quantity = 1, bool hotbar = false)
        {
            if (!GetEntry(slotIndex, hotbar, out ItemSO item, out int amount)) return false;
            if (item == null || amount <= 0) return false;

            int dropQty = Mathf.Min(quantity, amount);

            if (item.itemPrefab != null)
            {
                Camera cam = Camera.main;
                if (cam == null) cam = FindFirstObjectByType<Camera>();

                Vector3 spawnPos;
                if (dropPoint != null)
                    spawnPos = dropPoint.position;
                else if (cam != null)
                    spawnPos = cam.transform.position + cam.transform.forward * 1.5f;
                else
                    spawnPos = transform.position;

                GameObject dropped = Instantiate(item.itemPrefab, spawnPos, Quaternion.identity);
                dropped.SetActive(false);

                MeshCollider[] meshCols = dropped.GetComponentsInChildren<MeshCollider>();
                foreach (var mc in meshCols)
                    if (!mc.convex) mc.convex = true;

                Item worldItem = dropped.GetComponent<Item>();
                if (worldItem == null)
                    worldItem = dropped.AddComponent<Item>();
                worldItem.item = item;
                worldItem.amount = dropQty;

                Rigidbody rb = dropped.GetComponentInChildren<Rigidbody>();
                if (rb == null) rb = dropped.AddComponent<Rigidbody>();
                rb.useGravity = true;

                Collider col = dropped.GetComponentInChildren<Collider>();
                if (col == null) dropped.AddComponent<SphereCollider>();

                dropped.SetActive(true);

                Vector3 dir = (dropPoint != null ? dropPoint.forward : transform.forward) + Vector3.up * 0.5f;
                rb.linearVelocity = dir.normalized * dropForce;
            }

            int left = amount - dropQty;
            if (left <= 0)
                SetEntry(slotIndex, hotbar, null, 0);
            else
                SetEntry(slotIndex, hotbar, item, left);

            OnInventoryChanged?.Invoke();
            RefreshAllSlotUIs();
            UpdateHandItem();
            return true;
        }

        public void DropSelectedItem()
        {
            if (selectedHotbar < 0 || selectedHotbar >= hotbarCount) return;
            if (hotbarItems[selectedHotbar] == null) return;
            DropItem(selectedHotbar, 1, true);
        }

        // ===== UI REFRESH =====

        public void RefreshAllSlotUIs()
        {
            for (int i = 0; i < panelSlots.Count && i < panelCount; i++)
                panelSlots[i].Display(dataItems[i], dataAmounts[i]);

            for (int i = 0; i < hotbarSlots.Count && i < hotbarCount; i++)
                hotbarSlots[i].Display(hotbarItems[i], hotbarAmounts[i]);
        }

        // ===== SAVE =====

        public string GetUniqueId() => uniqueId;

        public string CaptureState()
        {
            var data = new InventorySlotSaveData
            {
                panelSize = panelCount,
                hotbarSize = hotbarCount,
                selectedHotbar = selectedHotbar
            };

            for (int i = 0; i < panelCount; i++)
                data.panel.Add(new InventorySlotItemSave
                {
                    itemName = dataItems[i] != null ? dataItems[i].itemName : null,
                    amount = dataItems[i] != null ? dataAmounts[i] : 0
                });

            for (int i = 0; i < hotbarCount; i++)
                data.hotbar.Add(new InventorySlotItemSave
                {
                    itemName = hotbarItems[i] != null ? hotbarItems[i].itemName : null,
                    amount = hotbarItems[i] != null ? hotbarAmounts[i] : 0
                });

            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            var data = JsonUtility.FromJson<InventorySlotSaveData>(json);
            if (data == null) return;

            for (int i = 0; i < panelCount; i++)
            {
                dataItems[i] = null;
                dataAmounts[i] = 0;
            }
            for (int i = 0; i < hotbarCount; i++)
            {
                hotbarItems[i] = null;
                hotbarAmounts[i] = 0;
            }

            for (int i = 0; i < data.panel.Count && i < panelCount; i++)
            {
                var entry = data.panel[i];
                if (entry == null) continue;
                ItemSO item = ResolveItem(entry.itemName);
                if (item != null && entry.amount > 0)
                {
                    dataItems[i] = item;
                    dataAmounts[i] = Mathf.Min(entry.amount, item.maxStackSize);
                }
            }

            for (int i = 0; i < data.hotbar.Count && i < hotbarCount; i++)
            {
                var entry = data.hotbar[i];
                if (entry == null) continue;
                ItemSO item = ResolveItem(entry.itemName);
                if (item != null && entry.amount > 0)
                {
                    hotbarItems[i] = item;
                    hotbarAmounts[i] = Mathf.Min(entry.amount, item.maxStackSize);
                }
            }

            if (data.selectedHotbar >= 0 && data.selectedHotbar < hotbarCount)
                selectedHotbar = data.selectedHotbar;

            OnInventoryChanged?.Invoke();
            RefreshAllSlotUIs();
            UpdateHandItem();
        }

        private ItemSO ResolveItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName) || itemLookupByName == null) return null;
            return itemLookupByName.TryGetValue(itemName, out ItemSO item) ? item : null;
        }

        public ItemSO FindItemByName(string itemName)
        {
            return ResolveItem(itemName);
        }
    }
}
