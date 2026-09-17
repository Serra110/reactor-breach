using UnityEngine;

/// <summary>
/// Continuous signal lever with bidirectional sync to a ReactorController channel.
///
/// When NOT dragging: value = reactor.GetChannel(channel)  (reads simulation)
/// When dragging:     value → reactor.SetChannel(channel)  (player input)
///
/// Backward compat: outputNode still emits ReactorPacket for the old Node/Wire system.
/// </summary>
[ExecuteAlways]
public class SignalLever : SignalModule
{
    [Header("Channel")]
    [Tooltip("Canal do reactor que esta alavanca controla.")]
    public ReactorChannelDef.Select channel = ReactorChannelDef.Select.RodInsertion;

    [Tooltip("Direct reference to the reactor. Auto-found in parent if null.")]
    public ReactorController reactor;

    [Header("Output (backward compat)")]
    public Node outputNode;

    [Header("Value")]
    [Tooltip("Current value 0-1. Lerp angles are applied automatically.")]
    [Range(0f, 1f)]
    public float value;

    [Header("Physical Drag")]
    public bool enableMouseDrag = true;
    [Tooltip("Value change per screen pixel moved vertically.")]
    public float dragSensitivity = 0.1000000f;      
    [Tooltip("false: mouse up increases value; true: mouse down increases value.")]
    public bool invertDrag = false;

    [Header("Quantization")]
    [Tooltip("Snap value to discrete steps (e.g. 5% increments for reactor rods).")]
    public bool quantize = true;
    [Tooltip("Number of steps between 0 and 1. 20 = 5% per step.")]
    public int stepsPerUnit = 20;

    [Header("Visual Rotation")]
    [Tooltip("Local X rotation at value 0 (inserted/control rods in).")]
    public float minAngle = 90f;
    [Tooltip("Local X rotation at value 1 (retracted/control rods out).")]
    public float maxAngle = -90f;
    public Transform leverArm;

    [Header("Sync")]
    [Tooltip("When not dragging, continuously sync value from the reactor channel.")]
    public bool syncFromReactor = true;

    [Header("Debug")]
    public bool debugLog;

    public bool IsDragging { get; private set; }

    private float lastSyncValue;
    private Vector3 lastDragMousePosition;
    private float dragStartValue;
    private float lastVisualAngle;

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceOutputType(outputNode);
        if (reactor == null)
            reactor = GetComponentInParent<ReactorController>();
        UpdateVisual();
    }

    private void OnValidate()
    {
        UpdateVisual();
    }

    private void Awake()
    {
        if (reactor == null)
            reactor = GetComponentInParent<ReactorController>();
    }

    /// <summary>
    /// Sets the lever value 0-1 and immediately pushes to the reactor channel.
    /// </summary>
    public void SetValue(float newValue)
    {
        newValue = Mathf.Clamp01(newValue);
        if (quantize && stepsPerUnit > 1)
            newValue = Mathf.Round(newValue * stepsPerUnit) / stepsPerUnit;
        value = newValue;
        if (reactor != null)
            reactor.SetChannel((int)channel, value);
        Emit();
        UpdateVisual();
    }

    /// <summary>
    /// Begins a physical drag operation.
    /// </summary>
    public void BeginDrag()
    {
        if (!enableMouseDrag) return;
        IsDragging = true;
        dragStartValue = value;
        lastDragMousePosition = Input.mousePosition;
    }

    /// <summary>
    /// Applies vertical mouse movement to the lever value.
    /// </summary>
    public void UpdateDrag(float mouseDelta)
    {
        if (!IsDragging || !enableMouseDrag) return;
        float direction = invertDrag ? -1f : 1f;
        SetValue(value + mouseDelta * dragSensitivity * direction);
    }

    /// <summary>
    /// Updates the lever from the current absolute cursor position.
    /// </summary>
    public void UpdateDragFromMousePosition(Vector3 mousePosition)
    {
        if (!IsDragging || !enableMouseDrag) return;
        float mouseDeltaY = mousePosition.y - lastDragMousePosition.y;
        lastDragMousePosition = mousePosition;
        UpdateDrag(mouseDeltaY);
    }

    /// <summary>
    /// Ends a physical drag operation.
    /// </summary>
    public void EndDrag()
    {
        if (IsDragging && debugLog)
        {
            bool movedUp = Input.mousePosition.y > lastDragMousePosition.y;
            Debug.Log($"[SignalLever] {ReactorChannelDef.GetName((int)channel)}: " +
                $"rato {(movedUp ? "para CIMA" : "para BAIXO")} → " +
                $"{dragStartValue * 100f:F0}% → {value * 100f:F0}% " +
                $"(angle {lastVisualAngle:F1}).");
        }
        IsDragging = false;
    }

    public override void OnEnteract()
    {
        if (controlUI != null)
            base.OnEnteract();
    }

    private void Update()
    {
        // Sync from reactor when not dragging
        if (syncFromReactor && !IsDragging && reactor != null && Application.isPlaying)
        {
            float reactorValue = reactor.GetChannel((int)channel);
            if (Mathf.Abs(reactorValue - lastSyncValue) > 0.001f)
            {
                value = reactorValue;
                lastSyncValue = value;
            }
        }

        Emit();
        UpdateVisual();
    }

    /// <summary>
    /// Emits the current value as a ReactorPacket on the outputNode (backward compat).
    /// </summary>
    private void Emit()
    {
        if (outputNode == null || !Application.isPlaying) return;

        int intValue = Mathf.RoundToInt(value * 100f);
        int packet = ReactorPacket.Make(ReactorPacket.TargetRods, ReactorPacket.ActionSet, intValue);
        outputNode.PublishValue(packet);
    }

    private void UpdateVisual()
    {
        if (leverArm == null) return;
        float angle = Mathf.Lerp(minAngle, maxAngle, value);
        leverArm.localRotation = Quaternion.Euler(angle, 0f, 0f);
        lastVisualAngle = angle;
    }
}
