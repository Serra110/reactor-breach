using UnityEngine;
using DG.Tweening;

public class Switch : ElectricDevice
{
    public bool switchState;
    public Transform switchHandle;
    public Tween switchTween;

    /// <summary>
    /// Applies the initial switch visual and electrical state.
    /// </summary>
    public override void Start()
    {
        base.Start();
        ApplySwitchState();
    }

    /// <summary>
    /// Recalculates the switch output when its input changes.
    /// </summary>
    public override void OnTotalInputChanged(int newTotalInput)
    {
        base.OnTotalInputChanged(newTotalInput);
        ApplySwitchState();
    }

    /// <summary>
    /// Requests a synchronized toggle through the network switch companion.
    /// </summary>
    public override void OnEnteract()
    {
        NetworkSwitch networkSwitch = GetComponent<NetworkSwitch>();
        if (networkSwitch != null)
        {
            networkSwitch.RequestToggle();
            return;
        }

        SetNetworkState(!switchState);
    }

    /// <summary>
    /// Applies a state received from the server or used by offline gameplay.
    /// </summary>
    public void SetNetworkState(bool newState)
    {
        switchState = newState;
        ApplySwitchState();
    }

    private void ApplySwitchState()
    {
        int availableOutput = switchState ? Mathf.Max(0, totalInput) : 0;
        DistributeOutput(availableOutput);
        HandleSwitchAnimation();
    }

    private void DistributeOutput(int availableOutput)
    {
        if (outputPorts == null)
            return;

        int validOutputCount = 0;
        foreach (Port port in outputPorts)
        {
            if (port != null)
                validOutputCount++;
        }

        if (validOutputCount == 0)
            return;

        int valuePerOutput = availableOutput / validOutputCount;
        int remainder = availableOutput % validOutputCount;
        int outputIndex = 0;
        foreach (Port port in outputPorts)
        {
            if (port == null)
                continue;

            port.SetValue(valuePerOutput + (outputIndex < remainder ? 1 : 0));
            outputIndex++;
        }
    }

    /// <summary>
    /// Animates the switch handle to match the current state.
    /// </summary>
    public void HandleSwitchAnimation()
    {
        if (switchHandle == null)
            return;

        if (switchTween != null)
            switchTween.Kill();

        Quaternion targetRotation = switchState
            ? Quaternion.Euler(-90f, 0f, 0f)
            : Quaternion.Euler(90f, 0f, 0f);
        switchTween = switchHandle.DOLocalRotateQuaternion(targetRotation, 0.5f);
    }
}
