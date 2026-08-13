using System.Collections.Generic;
using Mirror;
using UnityEngine;

public sealed class NetworkWire : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnInputPathChanged))]
    private string inputPath;

    [SyncVar(hook = nameof(OnOutputPathChanged))]
    private string outputPath;

    private readonly SyncList<Vector3> syncedPoints = new SyncList<Vector3>();
    private Wire localWire;
    private bool connectionApplied;

    public string InputPath => inputPath;
    public string OutputPath => outputPath;

    private void Awake()
    {
        localWire = GetComponent<Wire>();
    }

    public override void OnStartClient()
    {
        connectionApplied = false;
        ApplyConnection();
    }

    private void Update()
    {
        if (!connectionApplied && !string.IsNullOrEmpty(inputPath) && !string.IsNullOrEmpty(outputPath))
            ApplyConnection();
    }

    [Server]
    public void ConfigureServer(string newInputPath, string newOutputPath, IReadOnlyList<Vector3> points)
    {
        inputPath = newInputPath;
        outputPath = newOutputPath;
        syncedPoints.Clear();
        if (points != null)
        {
            for (int i = 0; i < points.Count; i++)
                syncedPoints.Add(points[i]);
        }
        ApplyConnection();
    }

    [Server]
    public void ServerRemove()
    {
        if (NetworkServer.active)
            NetworkServer.Destroy(gameObject);
    }

    private void OnInputPathChanged(string _, string __)
    {
        ApplyConnection();
    }

    private void OnOutputPathChanged(string _, string __)
    {
        ApplyConnection();
    }

    private void ApplyConnection()
    {
        if (localWire == null || string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
            return;

        Port inputPort = FindPort(inputPath);
        Port outputPort = FindPort(outputPath);
        if (inputPort == null || outputPort == null)
            return;

        List<Vector3> points = new List<Vector3>(syncedPoints);
        localWire.CreateConnection(inputPort, outputPort, points);
        connectionApplied = true;
    }

    private static Port FindPort(string path)
    {
        GameObject target = GameObject.Find(path);
        if (target == null)
            return null;
        return target.GetComponent<Port>() ?? target.GetComponentInChildren<Port>(true);
    }
}
