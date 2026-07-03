using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    private Dictionary<string, int> resources =
        new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("InventoryManager ativo");
        }

        resources["Metal"] = 0;
        resources["Uranium"] = 0;
        resources["Money"] = 0;
    }

    public void AddResource(string resourceName, int amount)
    {
        if (!resources.ContainsKey(resourceName))
        {
            resources[resourceName] = 0;
        }

        resources[resourceName] += amount;

        Debug.Log(resourceName + ": " + resources[resourceName]);
    }

    public bool RemoveResource(string resourceName, int amount)
    {
        if (!resources.ContainsKey(resourceName))
            return false;

        if (resources[resourceName] < amount)
            return false;

        resources[resourceName] -= amount;

        return true;
    }

    public int GetResourceAmount(string resourceName)
    {
        if (!resources.ContainsKey(resourceName))
            return 0;

        return resources[resourceName];
    }
}