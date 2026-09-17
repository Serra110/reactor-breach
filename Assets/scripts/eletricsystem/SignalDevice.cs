using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base para dispositivos de sinal (análogo ao ElectricDevice).
/// Tem inputNodes e outputNodes configuráveis no inspector.
/// Por defeito, encaminha pacotes recebidos nas entradas para todas as saídas.
/// Subclasses podem override OnInputPacket para processar/transformar pacotes.
/// </summary>
public class SignalDevice : EletricUnit
{
    [Header("Signal Nodes")]
    public List<Node> inputNodes = new List<Node>();
    public List<Node> outputNodes = new List<Node>();

    [Header("Packet Routing")]
    [Tooltip("Se ativo, pacotes recebidos nos inputNodes são reencaminhados para todos os outputNodes.")]
    public bool forwardPackets = true;

    protected virtual void OnEnable()
    {
        SetupNodeTypes();
        SubscribeInputs();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeInputs();
    }

    private void SetupNodeTypes()
    {
        if (inputNodes != null)
            foreach (var node in inputNodes)
                if (node != null) node.type = PortType.Input;

        if (outputNodes != null)
            foreach (var node in outputNodes)
                if (node != null) node.type = PortType.Output;
    }

    private void SubscribeInputs()
    {
        if (inputNodes == null) return;
        foreach (var node in inputNodes)
            if (node != null) node.onValueChanged += OnInputPacket;
    }

    private void UnsubscribeInputs()
    {
        if (inputNodes == null) return;
        foreach (var node in inputNodes)
            if (node != null) node.onValueChanged -= OnInputPacket;
    }

    protected virtual void OnInputPacket(int packet)
    {
        if (forwardPackets)
            ForwardToOutputs(packet);
    }

    protected void ForwardToOutputs(int packet)
    {
        if (outputNodes == null) return;
        foreach (var node in outputNodes)
            if (node != null) node.PublishValue(packet);
    }

    protected void EmitToOutputs(int packet)
    {
        ForwardToOutputs(packet);
    }
}
