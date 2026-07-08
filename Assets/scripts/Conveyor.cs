using System.Collections.Generic;
using UnityEngine;

public class Conveyor : MonoBehaviour
{
    private struct ResourceTransfer
    {
        public string itemName;
        public int amount;

        public ResourceTransfer(string itemName, int amount)
        {
            this.itemName = itemName;
            this.amount = amount;
        }
    }

    public Conveyor nextConveyor;
    public Storage targetStorage;

    private readonly Queue<ResourceTransfer> items = new Queue<ResourceTransfer>();

    public float transferInterval = 1f;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < transferInterval)
            return;

        timer = 0f;

        MoveItem();
    }

    public void AddItem(string itemName, int amount)
    {
        if (amount <= 0)
            return;

        items.Enqueue(new ResourceTransfer(itemName, amount));
    }

    void MoveItem()
    {
        if (items.Count == 0)
            return;

        ResourceTransfer transfer = items.Dequeue();

        if (targetStorage != null)
        {
            if (!targetStorage.AddItem(transfer.itemName, transfer.amount))
            {
                Debug.LogWarning("Conveyor: Storage is full or cannot accept the transfer.");
            }

            return;
        }

        if (nextConveyor != null)
        {
            nextConveyor.AddItem(transfer.itemName, transfer.amount);
        }
        else
        {
            // try to find a nearby storage to deposit if there is no linked next conveyor
            Storage s = FindNearbyStorage();
            if (s != null)
            {
                if (!s.AddItem(transfer.itemName, transfer.amount))
                    Debug.LogWarning("Conveyor: Nearby storage rejected the transfer.");
                return;
            }

            // fallback: add to player inventory if available
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddResource(transfer.itemName, transfer.amount);
                return;
            }
        }
    }

    Storage FindNearbyStorage(float radius = 1.5f)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
        foreach (var c in cols)
        {
            var storage = c.GetComponentInParent<Storage>();
            if (storage != null)
                return storage;
        }

        return null;
    }
}