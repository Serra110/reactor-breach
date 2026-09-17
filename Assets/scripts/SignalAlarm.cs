using UnityEngine;

/// <summary>
/// Alarme: observa um canal de telemetria e dispara quando cruza um limiar.
/// Pode acender luz, tocar sirene e (opcional) enviar um comando automático
/// — ex: TEMP >= 90 → SCRAM automático.
/// </summary>
public class SignalAlarm : SignalModule
{
    [Header("Input")]
    public Node inputNode;

    [Header("Trigger")]
    [Tooltip("Canal de telemetria a observar (ReactorChannel.*).")]
    public int channel = ReactorChannel.Temperature;
    [Tooltip("Valor que dispara o alarme.")]
    public int threshold = 80;
    [Tooltip("false: alarme quando valor >= threshold. true: quando valor <= threshold.")]
    public bool invert;
    [Tooltip("Zona morta para não piscar no limiar.")]
    public int hysteresis = 5;

    [Header("Visual / Audio")]
    public Light alarmLight;
    public Renderer alarmRenderer;
    public Color alarmColor = Color.red;
    public AudioSource siren;

    [Header("Auto Command (opcional)")]
    [Tooltip("Enviar um comando quando o alarme dispara (ex: SCRAM automático).")]
    public bool emitCommandOnAlarm;
    public Node outputNode;
    public int commandTarget = ReactorPacket.TargetReactor;
    public int commandAction = ReactorPacket.ActionTrigger;
    public int commandValue = ReactorPacket.TriggerScram;

    public bool IsActive { get; private set; }

    private Material cachedMaterial;
    private Color defaultMaterialColor = Color.white;
    private bool commandEmitted;

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceInputType(inputNode);
        ForceOutputType(outputNode);
        if (inputNode != null)
            inputNode.onValueChanged += OnPacket;

        if (alarmRenderer != null)
        {
            cachedMaterial = alarmRenderer.material;
            defaultMaterialColor = cachedMaterial.color;
        }
        ApplyVisual();
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
        int value = ReactorPacket.GetValue(packet);

        bool crossed = invert ? value <= threshold : value >= threshold;
        bool staysActive = invert ? value <= threshold + hysteresis : value >= threshold - hysteresis;
        bool shouldBeActive = IsActive ? staysActive : crossed;

        if (shouldBeActive != IsActive)
        {
            IsActive = shouldBeActive;
            commandEmitted = false;
            ApplyVisual();

            if (IsActive && emitCommandOnAlarm && outputNode != null && !commandEmitted)
            {
                outputNode.PublishValue(ReactorPacket.Make(commandTarget, commandAction, commandValue));
                commandEmitted = true;
            }
        }
    }

    private void ApplyVisual()
    {
        if (alarmLight != null)
            alarmLight.enabled = IsActive;

        if (cachedMaterial != null)
            cachedMaterial.color = IsActive ? alarmColor : defaultMaterialColor;

        if (siren != null)
        {
            if (IsActive && !siren.isPlaying) siren.Play();
            else if (!IsActive && siren.isPlaying) siren.Stop();
        }
    }
}
