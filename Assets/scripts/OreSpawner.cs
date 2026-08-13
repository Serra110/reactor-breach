using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class OreSpawner : MonoBehaviour
{
    public GameObject orePrefab;
    public Transform groundPlane;

    [Header("Ore Settings")]
    public ItemSO oreItem;
    public int amountPerOre = 10;

    public int amount = 20;
    public float minDistance = 2f;
    public float respawnCheckInterval = 5f;

    private readonly List<Vector3> spawnedPositions = new List<Vector3>();
    private readonly List<GameObject> spawnedOres = new List<GameObject>();
    private float respawnTimer;

    private void Start()
    {
        if (!NetworkServer.active || groundPlane == null || orePrefab == null)
            return;

        SpawnAllOres();
    }

    private void Update()
    {
        if (!NetworkServer.active || groundPlane == null || orePrefab == null)
            return;

        respawnTimer += Time.deltaTime;
        if (respawnTimer < respawnCheckInterval)
            return;
        respawnTimer = 0f;

        for (int i = spawnedOres.Count - 1; i >= 0; i--)
        {
            if (spawnedOres[i] == null)
            {
                spawnedOres.RemoveAt(i);
                spawnedPositions.RemoveAt(i);
            }
        }

        while (spawnedOres.Count < amount)
        {
            Bounds bounds = groundPlane.GetComponent<Renderer>().bounds;
            int attempts = 0;
            while (attempts < 100)
            {
                attempts++;
                Vector3 position = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.max.y + 0.1f,
                    Random.Range(bounds.min.z, bounds.max.z));
                if (IsFarEnough(position))
                {
                    SpawnOre(position);
                    break;
                }
            }
        }
    }

    private void SpawnAllOres()
    {
        Bounds bounds = groundPlane.GetComponent<Renderer>().bounds;
        int spawned = 0;
        int attempts = 0;

        while (spawned < amount && attempts < 1000)
        {
            attempts++;
            Vector3 position = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + 0.1f,
                Random.Range(bounds.min.z, bounds.max.z));
            if (IsFarEnough(position))
            {
                SpawnOre(position);
                spawned++;
            }
        }
    }

    private void SpawnOre(Vector3 position)
    {
        GameObject ore = Instantiate(orePrefab, position, Quaternion.identity);
        Ore oreComponent = ore.GetComponent<Ore>();
        if (oreComponent != null)
        {
            oreComponent.item = oreItem;
            oreComponent.amount = amountPerOre;
        }

        NetworkIdentity identity = ore.GetComponent<NetworkIdentity>();
        if (identity == null)
        {
            Destroy(ore);
            return;
        }

        NetworkServer.Spawn(ore);
        spawnedOres.Add(ore);
        spawnedPositions.Add(position);
    }

    private bool IsFarEnough(Vector3 position)
    {
        foreach (Vector3 previousPosition in spawnedPositions)
        {
            if (Vector3.Distance(position, previousPosition) < minDistance)
                return false;
        }
        return true;
    }
}
