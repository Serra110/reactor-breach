using System.Collections.Generic;
using UnityEngine;

public class Storage : MonoBehaviour
{
    [Header("Settings")]
    public int maxStorage = 200;
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

    // ===== PLAYER INTERACTION =====

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
}