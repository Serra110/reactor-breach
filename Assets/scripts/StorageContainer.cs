using System.Collections.Generic;
using UnityEngine;

public class Storage : MonoBehaviour
{
    [Header("Settings")]
    public int maxStorage = 200;
    public float interactRange = 3f;
    public bool enableDebugKeys = true;
    public int debugDepositAmount = 1;
    public int debugWithdrawAmount = 1;

    private Dictionary<string, int> items = new Dictionary<string, int>();

    private void Update()
    {
        if (!enableDebugKeys) return;

        if (Input.GetKeyDown(KeyCode.G))
            Deposit("Metal", debugDepositAmount);

        if (Input.GetKeyDown(KeyCode.H))
            Withdraw("Metal", debugWithdrawAmount);
    }

    private void OnMouseDown()
    {
        // Check if player is within interaction range
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > interactRange)
                return;
        }

        Deposit("Metal", debugDepositAmount);
    }

    // ===== CORE STORAGE =====

    public bool AddItem(string item, int amount)
    {
        if (GetTotal() + amount > maxStorage)
            return false;

        if (!items.ContainsKey(item))
            items[item] = 0;

        items[item] += amount;
        return true;
    }

    public bool RemoveItem(string item, int amount)
    {
        if (!items.ContainsKey(item))
            return false;

        if (items[item] < amount)
            return false;

        items[item] -= amount;

        if (items[item] <= 0)
            items.Remove(item);

        return true;
    }

    public int GetTotal()
    {
        int total = 0;

        foreach (var i in items)
            total += i.Value;

        return total;
    }

    // ===== PLAYER INTERACTION (old string-based) =====

    public void Deposit(string item, int amount)
    {
        var inv = InventoryManager.Instance;

        if (inv == null) return;

        if (!inv.HasResource(item, amount))
            return;

        if (AddItem(item, amount))
            inv.RemoveResource(item, amount);
    }

    public void Withdraw(string item, int amount)
    {
        var inv = InventoryManager.Instance;

        if (inv == null) return;

        if (!RemoveItem(item, amount))
            return;

        inv.AddResource(item, amount);
    }

    // ===== PLAYER INTERACTION (new ItemSO-based) =====

    public void Deposit(ItemSO item, int amount)
    {
        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return;

        if (!inv.HasItem(item, amount))
            return;

        if (AddItem(item.itemName, amount))
            inv.RemoveItem(item, amount);
    }

    public void Withdraw(ItemSO item, int amount)
    {
        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return;

        if (!RemoveItem(item.itemName, amount))
            return;

        inv.AddItem(item, amount);
    }

    // ===== MVP: E numa storage devolve tudo ao inventário do jogador (ItemSO) =====

    public void WithdrawAllToInventory()
    {
        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return;

        var keys = new List<string>(items.Keys);
        foreach (var key in keys)
        {
            if (!items.TryGetValue(key, out int amount) || amount <= 0)
                continue;

            ItemSO itemSO = inv.FindItemByName(key);
            if (itemSO == null) continue;

            int remaining = inv.AddItem(itemSO, amount);
            int taken = amount - remaining;
            if (taken > 0)
                RemoveItem(key, taken);
        }
    }
}