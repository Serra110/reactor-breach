using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class Wire : MonoBehaviour
{
    public Port inputPort;
    public Port outputPort;
    public LineRenderer line;

    private void Start()
    {
        if (inputPort != null && outputPort != null)
            CreateConnection();
    }


    private bool isConnected;

    public void CreateConnection()
    {
        if (inputPort == null || outputPort == null)
        {
            Debug.LogWarning("[Wire] CreateConnection requires both ports.");
            return;
        }

        ConnectPorts(inputPort, outputPort);
        UpdateLine(null);
    }

    public void CreateConnection(Port inputPort, Port outputPort, List<Vector3> points)
    {
        this.inputPort = inputPort;
        this.outputPort = outputPort;

        if (inputPort == null || outputPort == null)
            return;

        ConnectPorts(inputPort, outputPort);
        UpdateLine(points);
    }

    private void ConnectPorts(Port input, Port output)
    {
        if (!CanConnect(input, output))
        {
            Debug.LogWarning("[Wire] One of the ports already has an incompatible connection.");
            return;
        }

        if (!isConnected)
        {
            isConnected = true;
            input.RegisterConnection(this);
            output.RegisterConnection(this);
            output.onValueChanged += input.ReceiveValue;
        }

        input.SetValue(output.value);
    }

    private static bool CanConnect(Port input, Port output)
    {
        return input != null
            && output != null
            && input.type == PortType.Input
            && output.type == PortType.Output
            && input.CanAcceptConnection(null)
            && output.CanAcceptConnection(null);
    }

    private void UpdateLine(List<Vector3> points)
    {
        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
        }

        if (points != null && points.Count > 0)
        {
            line.positionCount = points.Count;
            line.SetPositions(points.ToArray());
            return;
        }

        if (inputPort == null || outputPort == null)
            return;

        line.positionCount = 2;
        line.SetPosition(0, inputPort.transform.position);
        line.SetPosition(1, outputPort.transform.position);
    }

    public bool CanCreatConnection()
    {
        return inputPort != null && outputPort != null;
    }

    public void ResetSelection()
    {
        inputPort = null;
        outputPort = null;
        isConnected = false;

        if (line != null)
            line.positionCount = 0;
    }

    public void ResetWire()
    {
        if (isConnected && inputPort != null && outputPort != null)
            outputPort.onValueChanged -= inputPort.ReceiveValue;

        if (inputPort != null)
            inputPort.UnregisterConnection(this);

        if (outputPort != null)
            outputPort.UnregisterConnection(this);

        inputPort = null;
        outputPort = null;
        isConnected = false;

        if (line != null)
            line.positionCount = 0;
    }

    public void RemoveWire()
    {
        ResetWire();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!isConnected || inputPort == null || outputPort == null)
            return;

        outputPort.onValueChanged -= inputPort.ReceiveValue;
        inputPort.UnregisterConnection(this);
        outputPort.UnregisterConnection(this);
    }
}
