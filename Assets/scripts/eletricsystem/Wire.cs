using System.Collections.Generic;
using UnityEngine;

public class Wire : MonoBehaviour
{
    public Port inputPort;
    public Port outputPort;
    public LineRenderer line;

    private bool isConnected;

    public void CreateConnection()
    {
        if (inputPort == null || outputPort == null)
        {
            Debug.LogWarning("[Wire] CreateConnection: inputPort/outputPort não atribuídos!");
            return;
        }

        if (!isConnected)
        {
            isConnected = true;
            inputPort.isConnected = true;
            outputPort.isConnected = true;

            inputPort.connectedWire = this;
            outputPort.connectedWire = this;

            outputPort.onValueChanged += inputPort.SetValue;
        }

        inputPort.SetValue(outputPort.value);

        if (line != null)
        {
            line.positionCount = 2;
            line.SetPosition(0, inputPort.transform.position);
            line.SetPosition(1, outputPort.transform.position);
        }
    }

    public void CreateConnection(Port inputPort, Port outputPort, List<Vector3> points)
    {
        this.inputPort = inputPort;
        this.outputPort = outputPort;

        if (inputPort == null || outputPort == null)
            return;

        if (!isConnected)
        {
            isConnected = true;
            inputPort.isConnected = true;
            outputPort.isConnected = true;

            inputPort.connectedWire = this;
            outputPort.connectedWire = this;

            outputPort.onValueChanged += inputPort.SetValue;
        }

        inputPort.SetValue(outputPort.value);

        if (line == null)
            line = gameObject.AddComponent<LineRenderer>();

        if (line.material == null)
            line.material = new Material(Shader.Find("Sprites/Default"));

        line.startWidth = 0.05f;
        line.endWidth = 0.05f;

        if (points != null && points.Count > 0)
        {
            line.positionCount = points.Count;
            line.SetPositions(points.ToArray());
        }
        else
        {
            line.positionCount = 2;
            line.SetPosition(0, inputPort.transform.position);
            line.SetPosition(1, outputPort.transform.position);
        }

        Debug.Log($"[Wire] Cabo criado e desenhado. points={points?.Count} line={line != null}");
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
        if (inputPort != null && outputPort != null)
        {
            outputPort.onValueChanged -= inputPort.SetValue;
        }

        if (inputPort != null) inputPort.isConnected = false;
        if (outputPort != null) outputPort.isConnected = false;

        if (inputPort != null && inputPort.connectedWire == this) inputPort.connectedWire = null;
        if (outputPort != null && outputPort.connectedWire == this) outputPort.connectedWire = null;

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
        if (isConnected && outputPort != null && inputPort != null)
        {
            outputPort.onValueChanged -= inputPort.SetValue;

            if (inputPort.connectedWire == this) inputPort.connectedWire = null;
            if (outputPort.connectedWire == this) outputPort.connectedWire = null;
            inputPort.isConnected = false;
            outputPort.isConnected = false;
        }
    }
}
