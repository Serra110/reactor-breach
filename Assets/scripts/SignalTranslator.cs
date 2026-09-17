using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SignalTranslation
{
    [Tooltip("Nota de referência (não usado pelo sistema).")]
    public string note;
    [Tooltip("Canal de telemetria a observar (0 = qualquer).")]
    public int fromChannel = ReactorChannel.Temperature;
    [Tooltip("Valor mínimo que dispara (incluído).")]
    public int fromMin = 80;
    [Tooltip("Valor máximo que dispara (incluído). 0 = sem máximo.")]
    public int fromMax = 0;
    public int toTarget = ReactorPacket.TargetReactor;
    public int toAction = ReactorPacket.ActionTrigger;
    public int toValue = ReactorPacket.TriggerScram;
}

/// <summary>
/// Tradutor / comparador: lê telemetria e converte em comandos.
/// O "Arduino" — permite automação simples sem lógica externa.
/// Ex: TEMP >= 90 → SCRAM. FLOW <= 20 → ligar bombas.
/// </summary>
public class SignalTranslator : SignalModule
{
    [Header("Input / Output")]
    public Node inputNode;
    public Node outputNode;

    [Header("Program")]
    public SignalTranslation[] bindings = new SignalTranslation[0];
    [Tooltip("Reencaminhar pacotes que não disparam nenhuma tradução.")]
    public bool passthrough = false;

    [Header("Display")]
    public Text displayText;

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceInputType(inputNode);
        ForceOutputType(outputNode);
        if (inputNode != null)
            inputNode.onValueChanged += OnPacket;
    }

    protected override void OnDisable()
    {
        if (inputNode != null)
            inputNode.onValueChanged -= OnPacket;
        base.OnDisable();
    }

    public void Send(int packet)
    {
        if (outputNode != null)
            outputNode.PublishValue(packet);
    }

    private void OnPacket(int packet)
    {
        if (displayText != null)
            displayText.text = ReactorPacket.Describe(packet);

        if (ReactorPacket.GetTarget(packet) == ReactorPacket.TargetTelemetry)
        {
            int channel = ReactorPacket.GetAction(packet);
            int value = ReactorPacket.GetValue(packet);

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    if (b == null) continue;
                    if (b.fromChannel != 0 && b.fromChannel != channel) continue;
                    if (value < b.fromMin) continue;
                    if (b.fromMax > 0 && value > b.fromMax) continue;

                    Send(ReactorPacket.Make(b.toTarget, b.toAction, b.toValue));
                    return;
                }
            }
        }

        if (passthrough)
            Send(packet);
    }
}
