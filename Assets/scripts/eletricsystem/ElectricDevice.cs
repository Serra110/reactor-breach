using System.Collections.Generic;
using UnityEngine;

public class ElectricDevice : EletricUnit
{
    public int deviceCost;
    public List<Port> inputPorts;
    public List<Port> outputPorts;
    public int totalInput;
    public bool isDeviceActive = false;

    /// <summary>
    /// Subscribes every configured input port and initializes the device output state.
    /// </summary>
    public virtual void Start()
    {
        if (inputPorts == null)
            inputPorts = new List<Port>();

        foreach (Port port in inputPorts)
        {
            if (port != null)
                port.onValueChanged += OnInputPortsValueChanged;
        }

        OnInputPortsValueChanged(0);
    }

    protected virtual void OnDestroy()
    {
        if (inputPorts == null)
            return;

        foreach (Port port in inputPorts)
        {
            if (port != null)
                port.onValueChanged -= OnInputPortsValueChanged;
        }
    }

    /// <summary>
    /// Recalculates the device input from every configured input port.
    /// </summary>
    public void OnInputPortsValueChanged(int portsNewValue)
    {
        if (inputPorts == null)
            return;

        int newTotalInput = 0;
        foreach (Port port in inputPorts)
        {
            if (port != null)
                newTotalInput += Mathf.Max(0, port.value);
        }

        OnTotalInputChanged(newTotalInput);
    }

    /// <summary>
    /// Updates device power state and distributes available power over all outputs.
    /// </summary>
    public virtual void OnTotalInputChanged(int newTotalInput)
    {
        totalInput = Mathf.Max(0, newTotalInput);

        int availableOutput = totalInput;
        bool shouldBeActive = deviceCost > 0 && totalInput >= deviceCost;
        if (deviceCost > 0)
            availableOutput = shouldBeActive ? totalInput - deviceCost : 0;

        if (isDeviceActive != shouldBeActive)
            OnDevicePowerStateChanged(shouldBeActive);

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

            int outputValue = valuePerOutput + (outputIndex < remainder ? 1 : 0);
            port.SetValue(outputValue);
            outputIndex++;
        }
    }

    /// <summary>
    /// Applies the device power-state change.
    /// </summary>
    public virtual void OnDevicePowerStateChanged(bool newState)
    {
        isDeviceActive = newState;
    }
}
