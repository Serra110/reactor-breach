using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class WireTool : NetworkBehaviour
{
    public Port detectedPort;
    public float detectingRange = 5f;
    public Wire tempWire;
    public Wire wirePrefab;
    public List<Vector3> wirePoints = new List<Vector3>();
    public float wireRemoveTimer;
    public KeyCode deleteWireKey = KeyCode.X;
    public float wireRemoveDuration = 1.5f;

    private void Start()
    {
        EnsureTemporaryWire();
    }

    private void Update()
    {
        if (!NetworkClient.active)
            return;

        DetectPort();
        EnsureTemporaryWire();

        if (Input.GetKeyDown(deleteWireKey) && detectedPort != null && detectedPort.connectedWire != null)
        {
            RequestRemoveWire(detectedPort.connectedWire);
            detectedPort.HidePort();
            detectedPort = null;
            wireRemoveTimer = 0f;
            wirePoints.Clear();
            return;
        }

        HandleLeftClick();
        HandleRightClick();
        UpdateTemporaryWirePreview();
    }

    private void EnsureTemporaryWire()
    {
        if (tempWire == null)
        {
            GameObject temporaryObject = new GameObject("Temporary Wire");
            tempWire = temporaryObject.AddComponent<Wire>();
        }

        if (tempWire.line == null)
        {
            tempWire.line = tempWire.gameObject.AddComponent<LineRenderer>();
            tempWire.line.material = new Material(Shader.Find("Sprites/Default"));
            tempWire.line.startWidth = 0.05f;
            tempWire.line.endWidth = 0.05f;
        }

        if (tempWire.line.positionCount > 0 && wirePoints.Count == 0)
            tempWire.line.positionCount = 0;
    }

    private Camera GetViewCamera()
    {
        Camera viewCamera = Camera.main;
        if (viewCamera != null)
            return viewCamera;
        return FindFirstObjectByType<Camera>();
    }

    private void DetectPort()
    {
        Camera viewCamera = GetViewCamera();
        if (viewCamera == null)
            return;

        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, detectingRange))
        {
            Port newPort = hit.transform.GetComponent<Port>();
            if (newPort == null)
                newPort = hit.transform.GetComponentInParent<Port>();
            if (newPort == null)
                newPort = hit.transform.GetComponentInChildren<Port>(true);

            if (newPort != null)
            {
                if (newPort == detectedPort)
                    return;
                if (detectedPort != null)
                    detectedPort.HidePort();
                detectedPort = newPort;
                detectedPort.ShowPort();
                return;
            }
        }

        if (detectedPort != null)
        {
            detectedPort.HidePort();
            detectedPort = null;
        }
    }

    private void HandleLeftClick()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (detectedPort != null)
        {
            if (detectedPort.isConnected)
                return;
            if (detectedPort.type == PortType.Input && tempWire.inputPort != null)
                return;
            if (detectedPort.type == PortType.Output && tempWire.outputPort != null)
                return;

            if (detectedPort.type == PortType.Input)
                tempWire.inputPort = detectedPort;
            else if (detectedPort.type == PortType.Output)
                tempWire.outputPort = detectedPort;

            AddWirePoint(detectedPort.transform.position);

            if (tempWire.CanCreatConnection())
            {
                CreateWire(tempWire.inputPort, tempWire.outputPort, wirePoints);
                tempWire.ResetSelection();
                wirePoints.Clear();
            }
            return;
        }

        if ((tempWire.inputPort == null) != (tempWire.outputPort == null) && IsThereStablePoint(out Vector3 stablePoint))
            AddWirePoint(stablePoint);
    }

    private void HandleRightClick()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (tempWire.line.positionCount == 0)
                return;

            if (tempWire.line.positionCount <= 2)
            {
                tempWire.ResetWire();
                wirePoints.Clear();
                tempWire.line.positionCount = 0;
            }
            else
            {
                wirePoints.RemoveAt(wirePoints.Count - 1);
                tempWire.line.positionCount = Mathf.Max(0, tempWire.line.positionCount - 1);
            }
        }

        if (Input.GetMouseButton(1) && detectedPort != null && detectedPort.isConnected)
        {
            wireRemoveTimer += Time.deltaTime;
            if (wireRemoveTimer >= wireRemoveDuration)
            {
                RequestRemoveWire(detectedPort.connectedWire);
                wireRemoveTimer = 0f;
                wirePoints.Clear();
            }
        }

        if (Input.GetMouseButtonUp(1))
            wireRemoveTimer = 0f;
    }

    private void UpdateTemporaryWirePreview()
    {
        if (tempWire == null || tempWire.line == null)
            return;
        if ((tempWire.inputPort == null) == (tempWire.outputPort == null))
            return;
        if (tempWire.line.positionCount == 0)
            return;

        if (!IsThereStablePoint(out Vector3 previewPoint))
        {
            Camera viewCamera = GetViewCamera();
            if (viewCamera == null)
                return;
            previewPoint = viewCamera.transform.position + viewCamera.transform.forward * detectingRange;
        }

        tempWire.line.SetPosition(tempWire.line.positionCount - 1, previewPoint);
    }

    public void CreateWire(Port inputPort, Port outputPort, List<Vector3> points)
    {
        if (inputPort == null || outputPort == null || wirePrefab == null)
            return;

        if (NetworkClient.active)
        {
            CmdCreateWire(GetHierarchyPath(inputPort.transform), GetHierarchyPath(outputPort.transform), points.ToArray());
            return;
        }

        Wire newWire = Instantiate(wirePrefab, Vector3.zero, Quaternion.identity);
        newWire.CreateConnection(inputPort, outputPort, points);
    }

    [Command(requiresAuthority = false)]
    private void CmdCreateWire(string inputPath, string outputPath, Vector3[] points, NetworkConnectionToClient sender = null)
    {
        Port inputPort = FindPort(inputPath);
        Port outputPort = FindPort(outputPath);
        if (inputPort == null || outputPort == null)
            return;
        if (inputPort.type != PortType.Input || outputPort.type != PortType.Output)
            return;
        if (inputPort.isConnected || outputPort.isConnected || wirePrefab == null)
            return;

        Wire newWire = Instantiate(wirePrefab, Vector3.zero, Quaternion.identity);
        NetworkWire networkWire = newWire.GetComponent<NetworkWire>();
        if (networkWire == null)
        {
            Destroy(newWire.gameObject);
            return;
        }

        List<Vector3> safePoints = new List<Vector3>();
        if (points != null)
        {
            int pointCount = Mathf.Min(points.Length, 32);
            for (int i = 0; i < pointCount; i++)
                safePoints.Add(points[i]);
        }
        networkWire.ConfigureServer(inputPath, outputPath, safePoints);
        NetworkServer.Spawn(newWire.gameObject);
    }

    private void RequestRemoveWire(Wire wire)
    {
        if (wire == null)
            return;

        NetworkWire networkWire = wire.GetComponent<NetworkWire>();
        if (NetworkClient.active && networkWire != null)
        {
            CmdRemoveWire(networkWire.InputPath, networkWire.OutputPath);
            return;
        }

        wire.RemoveWire();
    }

    [Command(requiresAuthority = false)]
    private void CmdRemoveWire(string inputPath, string outputPath, NetworkConnectionToClient sender = null)
    {
        Port inputPort = FindPort(inputPath);
        Port outputPort = FindPort(outputPath);
        if (inputPort == null || outputPort == null)
            return;

        Wire wire = inputPort.connectedWire != null ? inputPort.connectedWire : outputPort.connectedWire;
        NetworkWire networkWire = wire != null ? wire.GetComponent<NetworkWire>() : null;
        if (networkWire != null)
            NetworkServer.Destroy(networkWire.gameObject);
    }

    public bool IsThereStablePoint(out Vector3 stablePoint)
    {
        Camera viewCamera = GetViewCamera();
        if (viewCamera != null)
        {
            Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, detectingRange))
            {
                stablePoint = hit.point;
                return true;
            }
        }

        stablePoint = Vector3.zero;
        return false;
    }

    public void AddWirePoint(Vector3 newPoint)
    {
        wirePoints.Add(newPoint);
        if (tempWire == null || tempWire.line == null)
            return;

        if (tempWire.line.positionCount == 0)
        {
            tempWire.line.positionCount = 2;
            tempWire.line.SetPosition(0, newPoint);
        }
        else
        {
            tempWire.line.positionCount++;
            tempWire.line.SetPosition(tempWire.line.positionCount - 2, newPoint);
        }
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private static Port FindPort(string path)
    {
        GameObject target = GameObject.Find(path);
        if (target == null)
            return null;
        return target.GetComponent<Port>() ?? target.GetComponentInChildren<Port>(true);
    }
}
