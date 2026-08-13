using Mirror;
using ReactorBreach.InventorySystem;
using UnityEngine;

public class Ore : MonoBehaviour
{
    public ItemSO item;
    public int amount = 10;

    public void Collect()
    {
        Collect(Inventory.Instance);
    }

    public void Collect(Inventory inventory)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 2f);

        foreach (Collider collider in colliders)
        {
            Conveyor conveyor = collider.GetComponentInParent<Conveyor>();
            if (conveyor != null)
            {
                conveyor.AddItem(item != null ? item.itemName : "Metal", amount);
                DestroyCollectedObject();
                return;
            }
        }

        foreach (Collider collider in colliders)
        {
            Storage storage = collider.GetComponentInParent<Storage>();
            if (storage != null)
            {
                storage.AddItem(item != null ? item.itemName : "Metal", amount);
                DestroyCollectedObject();
                return;
            }
        }

        if (inventory != null && item != null)
        {
            int remaining = inventory.AddItem(item, amount);
            if (remaining < amount)
            {
                amount = remaining;
                if (amount <= 0)
                    DestroyCollectedObject();
                return;
            }
        }

        DestroyCollectedObject();
    }

    private void DestroyCollectedObject()
    {
        NetworkIdentity identity = GetComponent<NetworkIdentity>();
        if (NetworkServer.active && identity != null && identity.isServer)
            NetworkServer.Destroy(gameObject);
        else
            Destroy(gameObject);
    }
}
