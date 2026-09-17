using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3f;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            return;

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
                    int remaining = inventory.AddItem(worldItem.item, worldItem.amount);
                    if (remaining <= 0)
                        Destroy(worldItem.gameObject);
                    else
                        worldItem.amount = remaining;
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

            ReactorController reactor = hit.collider.GetComponentInParent<ReactorController>();
            if (reactor != null)
            {
                reactor.Interact();
                return;
            }

            Storage storage = hit.collider.GetComponentInParent<Storage>();
            if (storage != null)
            {
                storage.WithdrawAllToInventory();
                return;
            }

            SecurityDoor door = hit.collider.GetComponentInParent<SecurityDoor>();
            if (door != null)
            {
                door.Interact();
                return;
            }

            ContainmentCell cell = hit.collider.GetComponentInParent<ContainmentCell>();
            if (cell != null)
            {
                cell.Interact();
                return;
            }
        }
    }
}
