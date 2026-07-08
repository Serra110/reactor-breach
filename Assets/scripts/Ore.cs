using UnityEngine;

public class Ore : MonoBehaviour
{
    public string resourceName = "Metal";
    public int amount = 10;

    public void Collect()
    {
        // try conveyor first
        Collider[] cols = Physics.OverlapSphere(transform.position, 2f);
        foreach (var c in cols)
        {
            var conveyor = c.GetComponentInParent<Conveyor>();
            if (conveyor != null)
            {
                conveyor.AddItem(resourceName, amount);
                Destroy(gameObject);
                return;
            }
        }

        // then storage
        foreach (var c in cols)
        {
            var storage = c.GetComponentInParent<Storage>();
            if (storage != null)
            {
                storage.AddItem(resourceName, amount);
                Destroy(gameObject);
                return;
            }
        }

        // fallback to player inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddResource(resourceName, amount);

        Destroy(gameObject);
    }
}