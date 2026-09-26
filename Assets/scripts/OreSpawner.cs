using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class OreSpawner : MonoBehaviour
{
    public GameObject orePrefab;
    [Tooltip("Plano usado para o spawn inicial dos minerais.")]
    public Transform groundPlane;
    [Tooltip("Plano usado para recriar minerais depois de serem recolhidos. Se vazio, nao ha respawn.")]
    public Transform respawnGroundPlane;

    [Header("Ore Settings")]
    public ItemSO oreItem;
    public int amountPerOre = 10;

    public int amount = 20;
    public float minDistance = 2f;
    public float respawnCheckInterval = 5f;

    private readonly List<Vector3> spawnedPositions = new List<Vector3>();
    private readonly List<GameObject> spawnedOres = new List<GameObject>();
    private float respawnTimer;

    private Transform RespawnPlane => respawnGroundPlane;

    private void Start()
    {
        if ((!NetworkServer.active && !OfflineMvpBootstrap.IsOffline) || groundPlane == null || orePrefab == null)
            return;

        SpawnAllOres();
    }

    private void Update()
    {
        if ((!NetworkServer.active && !OfflineMvpBootstrap.IsOffline) || RespawnPlane == null || orePrefab == null)
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

        // FIX: o while anterior era um loop infinito na pratica. Se o plano de respawn
        // for pequeno demais para comportar `amount` minerais com `minDistance`, os 100
        // tentativas falhavam todas, nada era criado, Count continuava igual e o loop
        // nunca saia -> main thread bloqueado para sempre (freeze total no WebGL).
        // Agora cadaminerAL criado encerra a volta, com um teto de passes.
        if (spawnedOres.Count >= amount)
            return;

        Renderer planeRenderer = RespawnPlane.GetComponent<Renderer>();
        if (planeRenderer == null)
            return;

        Bounds bounds = planeRenderer.bounds;
        int passes = 0;
        while (spawnedOres.Count < amount && passes < 100)
        {
            passes++;

            int attempts = 0;
            bool spawnedOne = false;
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
                    spawnedOne = true;
                    break;
                }
            }

            if (!spawnedOne)
                break;
        }
    }

    private void SpawnAllOres()
    {
        Renderer renderer = groundPlane.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Bounds bounds = renderer.bounds;
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
        if (NetworkServer.active && identity != null)
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
