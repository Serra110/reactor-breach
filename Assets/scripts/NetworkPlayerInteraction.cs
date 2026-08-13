using Mirror;
using UnityEngine;
using ReactorBreach.InventorySystem;

public sealed class NetworkPlayerInteraction : NetworkBehaviour
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
        if (!isOwned || playerCamera == null)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            CmdInteract(playerCamera.transform.position, playerCamera.transform.forward);
    }

    [Command]
    private void CmdInteract(Vector3 origin, Vector3 direction)
    {
        Vector3 serverOrigin = transform.position + Vector3.up * 1.5f;
        Vector3 serverDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;

        if (Vector3.Distance(serverOrigin, origin) > 4f)
            origin = serverOrigin;

        if (!Physics.Raycast(serverOrigin, serverDirection, out RaycastHit hit, interactDistance))
            return;

        Ore ore = hit.collider.GetComponentInParent<Ore>();
        if (ore != null)
        {
            Inventory inventory = GetComponent<Inventory>();
            if (inventory == null)
                inventory = Inventory.Instance;
            ore.Collect(inventory);
            return;
        }

        Item worldItem = hit.collider.GetComponentInParent<Item>();
        if (worldItem != null && worldItem.item != null && worldItem.CanPickup())
        {
            Inventory inventory = GetComponent<Inventory>();
            if (inventory == null)
                inventory = Inventory.Instance;
            if (inventory != null)
            {
                int remaining = inventory.AddItem(worldItem.item, worldItem.amount);
                if (remaining < worldItem.amount)
                {
                    worldItem.amount = remaining;
                    if (worldItem.amount <= 0)
                        NetworkServer.Destroy(worldItem.gameObject);
                }
            }
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
            storage.WithdrawAllToInventory();
    }
}
