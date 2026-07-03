using System.Collections.Generic;
using UnityEngine;

public class OreSpawner : MonoBehaviour
{
    public GameObject orePrefab;
    public Transform groundPlane;

    public int amount = 20;
    public float minDistance = 2f;

    private List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        Debug.Log("OreSpawner STARTED");

        if (groundPlane == null)
        {
            Debug.LogError("Ground plane not assigned!");
            return;
        }

        Bounds bounds = groundPlane.GetComponent<Renderer>().bounds;

        int spawned = 0;
        int attempts = 0;

        while (spawned < amount && attempts < 1000)
        {
            attempts++;

            Vector3 pos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + 0.1f, // 🔥 exatamente em cima do plane
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (IsFarEnough(pos))
            {
                Instantiate(orePrefab, pos, Quaternion.identity);
                spawnedPositions.Add(pos);
                spawned++;
            }
        }

        Debug.Log("Spawned ores: " + spawned);
    }

    bool IsFarEnough(Vector3 pos)
    {
        foreach (Vector3 p in spawnedPositions)
        {
            if (Vector3.Distance(pos, p) < minDistance)
                return false;
        }
        return true;
    }
}