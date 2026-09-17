using System.Collections;
using UnityEngine;

public enum SignalButtonMode
{
    Momentary,
    Toggle
}

/// <summary>
/// Sends a discrete value to a ReactorController channel when pressed.
///
/// Momentary: sets channel to setValue on press.
/// Toggle:    sets channel to setValue on first press, toggleOffValue on next press.
///
/// Backward compat: outputNode still emits ReactorPacket for the old Node/Wire system.
/// </summary>
public class SignalButton : SignalModule
{
    [Header("Channel")]
    [Tooltip("Canal do reactor que este botão controla.")]
    public ReactorChannelDef.Select channel = ReactorChannelDef.Select.Scram;

    [Tooltip("Direct reference to the reactor. Auto-found in parent if null.")]
    public ReactorController reactor;

    [Header("Output (backward compat)")]
    public Node outputNode;

    [Header("Mode")]
    public SignalButtonMode mode = SignalButtonMode.Momentary;
    public bool toggleState;

    [Header("Value")]
    [Tooltip("Value written to the channel on press (0-1).")]
    [Range(0f, 1f)]
    public float setValue = 1f;

    [Tooltip("Value written to the channel on toggle-off (0-1).")]
    [Range(0f, 1f)]
    public float toggleOffValue = 0f;

    [Header("Visual Press")]
    public Transform buttonCap;
    [Tooltip("Axis in the buttonCap local space. Down is normally correct for a press.")]
    public Vector3 pressDirection = Vector3.down;
    [Tooltip("Transforms pressDirection by buttonCap.localRotation, producing the correct X/Y movement for tilted buttons.")]
    public bool useButtonLocalDirection = true;
    public float pressDistance = 0.05f;
    public float pressDuration = 0.08f;

    [Header("Debug")]
    public bool debugLog;

    private Vector3 initialButtonPosition;
    private Coroutine pressRoutine;

    private void Awake()
    {
        CacheButtonPosition();
        if (reactor == null)
            reactor = GetComponentInParent<ReactorController>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceOutputType(outputNode);
        if (reactor == null)
            reactor = GetComponentInParent<ReactorController>();
    }

    /// <summary>
    /// Sends the configured value to the reactor channel.
    /// </summary>
    public void Press()
    {
        if (reactor == null) return;

        float targetValue;
        if (mode == SignalButtonMode.Toggle)
        {
            toggleState = !toggleState;
            targetValue = toggleState ? setValue : toggleOffValue;
        }
        else
        {
            targetValue = setValue;
        }

        reactor.SetChannel((int)channel, targetValue);
        EmitPacket(targetValue);

        if (debugLog)
            Debug.Log($"[SignalButton] {ReactorChannelDef.GetName((int)channel)} → {targetValue:F2}");

        if (buttonCap != null)
        {
            if (pressRoutine != null)
                StopCoroutine(pressRoutine);
            pressRoutine = StartCoroutine(PressAnimation());
        }
    }

    /// <summary>
    /// Sets a toggle button state without simulating a physical press.
    /// </summary>
    public void SetToggleState(bool isOn)
    {
        if (mode != SignalButtonMode.Toggle || toggleState == isOn)
            return;

        toggleState = isOn;
        float targetValue = toggleState ? setValue : toggleOffValue;
        if (reactor != null)
            reactor.SetChannel((int)channel, targetValue);
        EmitPacket(targetValue);
    }

    public override void OnEnteract()
    {
        Press();
    }

    private void EmitPacket(float value)
    {
        if (outputNode == null || !Application.isPlaying) return;
        int intValue = Mathf.RoundToInt(value * 100f);
        int packet = ReactorPacket.Make(ReactorPacket.TargetReactor, ReactorPacket.ActionSet, intValue);
        outputNode.PublishValue(packet);
    }

    private void CacheButtonPosition()
    {
        if (buttonCap != null)
            initialButtonPosition = buttonCap.localPosition;
    }

    private Vector3 GetPressOffset()
    {
        if (buttonCap == null)
            return Vector3.zero;

        Vector3 direction = pressDirection.sqrMagnitude > 0f
            ? pressDirection.normalized
            : Vector3.down;

        if (useButtonLocalDirection)
            direction = buttonCap.localRotation * direction;

        return direction.normalized * pressDistance;
    }

    private IEnumerator PressAnimation()
    {
        if (buttonCap == null)
            yield break;

        buttonCap.localPosition = initialButtonPosition;
        Vector3 pressedPosition = initialButtonPosition + GetPressOffset();
        float duration = Mathf.Max(0.01f, pressDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            buttonCap.localPosition = Vector3.Lerp(initialButtonPosition, pressedPosition, elapsed / duration);
            yield return null;
        }

        buttonCap.localPosition = pressedPosition;
        yield return new WaitForSeconds(0.08f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            buttonCap.localPosition = Vector3.Lerp(pressedPosition, initialButtonPosition, elapsed / duration);
            yield return null;
        }

        buttonCap.localPosition = initialButtonPosition;
        pressRoutine = null;
    }
}
