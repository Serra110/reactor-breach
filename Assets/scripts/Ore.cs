using UnityEngine;

public class Ore : MonoBehaviour
{
    public string resourceName = "Metal";
    public int amount = 10;

    public void Collect()
    {
        InventoryManager.Instance.AddResource(resourceName, amount);

        Destroy(gameObject);
    }
}