using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Signal splitter: one input forwards each packet to multiple outputs.
/// </summary>
public class SignalSplitter : SignalModule
{
    [Header("Input")]
    public Node inputNode;

    [Header("Outputs")]
    public Node outputNodePrefab;
    public int outputCount = 4;
    public float outputSpacing = 0.25f;
    public List<Node> signalOutputNodes = new List<Node>();

    private void Awake()
    {
        EnsureOutputs();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceInputType(inputNode);
        if (inputNode != null)
            inputNode.onValueChanged += OnPacket;
    }

    protected override void OnDisable()
    {
        if (inputNode != null)
            inputNode.onValueChanged -= OnPacket;
        base.OnDisable();
    }

    private void EnsureOutputs()
    {
        if (signalOutputNodes == null)
            signalOutputNodes = new List<Node>();

        if (signalOutputNodes.Count > 0)
        {
            foreach (Node node in signalOutputNodes)
                if (node != null)
                    node.type = PortType.Output;
            return;
        }

        if (outputNodePrefab == null)
            return;

        for (int index = 0; index < outputCount; index++)
        {
            float offset = outputCount > 1
                ? (index - (outputCount - 1) * 0.5f) * outputSpacing
                : 0f;
            Node node = Instantiate(outputNodePrefab, transform);
            node.name = "SignalOutput_" + (index + 1);
            node.type = PortType.Output;
            node.transform.localPosition = new Vector3(offset, 0f, 0f);
            node.ResetPort();
            node.transform.localScale = Vector3.one * 0.3f;
            signalOutputNodes.Add(node);
        }
    }

    private void OnPacket(int packet)
    {
        foreach (Node node in signalOutputNodes)
            if (node != null)
                node.PublishValue(packet);
    }
}
