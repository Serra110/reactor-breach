using System.Collections.Generic;
using UnityEngine;

public class NodeSplitter : ElectricDevice
{
    [Tooltip("Número de nós de saída a criar se outputPortPrefab estiver atribuído.")]
    public int outputCount = 5;

    [Tooltip("Prefab do nó (usa Port.prefab). Cria as entradas/saídas automaticamente.")]
    public Port outputPortPrefab;

    [Tooltip("Espaçamento entre nós de saída criados automaticamente.")]
    public float outputSpacing = 0.25f;

    private void Awake()
    {
        deviceCost = 0;
        EnsureNodes();
    }

    private void EnsureNodes()
    {
        if (outputPortPrefab == null)
            return;

        if (inputPorts == null || inputPorts.Count == 0)
        {
            inputPorts = new List<Port>();
            Port inputPort = Instantiate(outputPortPrefab, transform);
            inputPort.name = "NodeInput";
            inputPort.type = PortType.Input;
            inputPort.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            inputPort.ResetPort();
            inputPort.SetBaseScale(0.35f);
            inputPorts.Add(inputPort);
        }

        if (outputPorts == null || outputPorts.Count == 0)
        {
            outputPorts = new List<Port>();
            for (int i = 0; i < outputCount; i++)
            {
                float offset = outputCount > 1 ? (i - (outputCount - 1) * 0.5f) * outputSpacing : 0f;
                Port port = Instantiate(outputPortPrefab, transform);
                port.name = "NodeOutput_" + (i + 1);
                port.type = PortType.Output;
                port.transform.localPosition = new Vector3(offset, 0.4f, 0f);
                port.ResetPort();
                port.SetBaseScale(0.35f);
                outputPorts.Add(port);
            }
        }
    }
}
