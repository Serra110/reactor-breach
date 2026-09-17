using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combines multiple signal inputs into one output bus.
/// Use it only when a build explicitly needs a visible multi-input module.
/// </summary>
public class SignalMerger : SignalModule
{
    [Header("Inputs")]
    public Node inputNodePrefab;
    public int inputCount = 4;
    public float inputSpacing = 0.25f;
    public List<Node> mergerInputNodes = new List<Node>();

    [Header("Output")]
    public Node outputNode;

    private void Awake()
    {
        EnsureInputs();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceOutputType(outputNode);
        SubscribeInputs();
    }

    protected override void OnDisable()
    {
        UnsubscribeInputs();
        base.OnDisable();
    }

    private void EnsureInputs()
    {
        if (mergerInputNodes == null)
            mergerInputNodes = new List<Node>();

        if (mergerInputNodes.Count > 0)
        {
            foreach (Node inputNode in mergerInputNodes)
                ForceInputType(inputNode);
            return;
        }

        if (inputNodePrefab == null)
            return;

        for (int index = 0; index < inputCount; index++)
        {
            float offset = inputCount > 1
                ? (index - (inputCount - 1) * 0.5f) * inputSpacing
                : 0f;
            Node inputNode = Instantiate(inputNodePrefab, transform);
            inputNode.name = "SignalInput_" + (index + 1);
            inputNode.type = PortType.Input;
            inputNode.transform.localPosition = new Vector3(offset, 0f, 0f);
            inputNode.ResetPort();
            inputNode.transform.localScale = Vector3.one * 0.3f;
            mergerInputNodes.Add(inputNode);
        }
    }

    private void SubscribeInputs()
    {
        if (mergerInputNodes == null)
            return;

        foreach (Node inputNode in mergerInputNodes)
        {
            ForceInputType(inputNode);
            if (inputNode != null)
                inputNode.onValueChanged += OnPacket;
        }
    }

    private void UnsubscribeInputs()
    {
        if (mergerInputNodes == null)
            return;

        foreach (Node inputNode in mergerInputNodes)
        {
            if (inputNode != null)
                inputNode.onValueChanged -= OnPacket;
        }
    }

    private void OnPacket(int packet)
    {
        if (outputNode != null)
            outputNode.PublishValue(packet);
    }
}
