using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            Item worldItem = hit.collider.GetComponentInParent<Item>();
            if (worldItem != null && worldItem.item != null && worldItem.CanPickup())
            {
                var inventory = ReactorBreach.InventorySystem.Inventory.Instance;
                if (inventory != null)
                {
                    inventory.AddItem(worldItem.item, worldItem.amount);
                    Destroy(worldItem.gameObject);
                }
                return;
            }

            Ore ore = hit.collider.GetComponentInParent<Ore>();
            if (ore != null)
            {
                ore.Collect();
                return;
            }

            SmelterController smelter = hit.collider.GetComponentInParent<SmelterController>();
            if (smelter != null)
            {
                smelter.Interact();
                return;
            }

            Storage storage = hit.collider.GetComponentInParent<Storage>();
            if (storage != null)
            {
                storage.WithdrawAllToInventory();
                return;
            }
        }
    }
}
