using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display: mostra o valor de um canal de telemetria.
/// Liga o inputNode a um splitter/bus e configura o canal.
/// </summary>
public class SignalDisplay : SignalModule
{
    [Header("Input")]
    public Node inputNode;

    [Header("Channel")]
    [Tooltip("Canal de telemetria a mostrar (ReactorChannel.*).")]
    public int channel = ReactorChannel.Temperature;

    [Header("Label")]
    [Tooltip("Prefixo no texto. Vazio = nome automático do canal.")]
    public string customLabel = "";

    [Header("Outputs")]
    [Tooltip("Text (canvas).")]
    public Text uiText;
    [Tooltip("TextMesh 3D (mais fácil de usar no mundo).")]
    public TextMesh worldText;

    private int lastValue = int.MinValue;

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceInputType(inputNode);
        if (inputNode != null)
            inputNode.onValueChanged += OnPacket;
        RefreshText();
    }

    protected override void OnDisable()
    {
        if (inputNode != null)
            inputNode.onValueChanged -= OnPacket;
        base.OnDisable();
    }

    private void OnPacket(int packet)
    {
        if (ReactorPacket.GetTarget(packet) != ReactorPacket.TargetTelemetry) return;
        if (ReactorPacket.GetAction(packet) != channel) return;
        lastValue = ReactorPacket.GetValue(packet);
        RefreshText();
    }

    private void RefreshText()
    {
        if (lastValue == int.MinValue) return;
        string label = string.IsNullOrEmpty(customLabel) ? ReactorChannel.GetName(channel) : customLabel;
        string text = label + " " + ReactorChannel.Format(channel, lastValue);
        if (uiText != null) uiText.text = text;
        if (worldText != null) worldText.text = text;
    }
}
