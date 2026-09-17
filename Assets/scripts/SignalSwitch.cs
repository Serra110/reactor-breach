using UnityEngine;

/// <summary>
/// Interruptor: estado persistente ON/OFF. Emite
/// (commandTarget, ENABLE/DISABLE) ou (commandTarget, SET, 100/0).
/// </summary>
public class SignalSwitch : SignalModule
{
    [Header("Output")]
    public Node outputNode;

    [Header("Command")]
    [Tooltip("Para onde envia (TargetCoolant, TargetFeedwater, TargetTurbine, TargetBreaker...).")]
    public int commandTarget = ReactorPacket.TargetCoolant;
    [Tooltip("true: emite ENABLE/DISABLE. false: emite SET 100/0.")]
    public bool useEnableDisable = true;

    [Header("State")]
    public bool isOn = false;

    [Header("Visual")]
    public Transform switchArm;
    public Vector3 rotationAxis = Vector3.right;
    public float maxAngle = 55f;

    private int lastEmitted = int.MinValue;

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceOutputType(outputNode);
        Emit(true);
    }

    /// <summary>Chamado pelo toggle da UI.</summary>
    public void SetOn(bool on)
    {
        if (isOn == on) return;
        isOn = on;
        Emit();
    }

    public void Toggle() => SetOn(!isOn);

    public void Emit(bool force = false)
    {
        if (outputNode == null) return;
        int packet = isOn
            ? (useEnableDisable
                ? ReactorPacket.Make(commandTarget, ReactorPacket.ActionEnable)
                : ReactorPacket.Make(commandTarget, ReactorPacket.ActionSet, 100))
            : (useEnableDisable
                ? ReactorPacket.Make(commandTarget, ReactorPacket.ActionDisable)
                : ReactorPacket.Make(commandTarget, ReactorPacket.ActionSet, 0));
        if (force || packet != lastEmitted)
        {
            lastEmitted = packet;
            outputNode.SetValue(packet);
        }
    }

    private void Update()
    {
        if (switchArm != null)
            switchArm.localRotation = Quaternion.Euler(rotationAxis * (isOn ? maxAngle : -maxAngle));
    }
}
