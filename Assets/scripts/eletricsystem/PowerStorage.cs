using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerStorage : EletricUnit
{
   public Port inputPort;
   public Port outputPort;

   public int maxCapacity;
   public int currentStorageAmount;

   public int maxOutput;
   public int currentOutput;

   [Min(0.1f)] public float refreshRate = 0.25f;
   private void Start()
   {
       StartCoroutine(refreshCoroutine());
   }

   IEnumerator refreshCoroutine()
    {
         while (true)
         {
            try
            {
                int incoming = inputPort != null ? inputPort.value : 0;
                currentStorageAmount += incoming;

                int connectedCost = GetConnectedDevicesCost();
                currentStorageAmount -= connectedCost;

                currentStorageAmount = Mathf.Clamp(currentStorageAmount, 0, maxCapacity);
                currentOutput = Mathf.Min(currentStorageAmount, maxOutput);
                if (outputPort != null)
                    outputPort.SetValue(currentOutput);

                if (DebugFlags.electricLogs) Debug.Log($"[PowerStorage] storage={currentStorageAmount} incoming={incoming} cost={connectedCost} output={currentOutput}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PowerStorage] ERRO no refresh: {e}");
            }
              yield return new WaitForSeconds(refreshRate);
         }
    }

    public int GetConnectedDevicesCost()
    {
        return SumDeviceCost(this, 0);
    }

    private int SumDeviceCost(EletricUnit from, int depth)
    {
        if (from == null || depth > 20)
            return 0;

        int total = 0;

        foreach (Port outPort in GetOutputPortsOf(from))
        {
            if (outPort == null || outPort.connectedWire == null)
                continue;

            Port nextInput = outPort.connectedWire.inputPort;
            if (nextInput == null)
                continue;

            ElectricDevice nextDevice = nextInput.GetComponentInParent<ElectricDevice>();
            if (nextDevice == null)
                continue;

            if (nextDevice.totalInput <= 0)
                continue;

            int downstreamCost = nextDevice.deviceCost + SumDeviceCost(nextDevice, depth + 1);

            int genSupply = GetGeneratorSupply(nextDevice);
            int batteryCount = GetActiveBatteryCount(nextDevice);
            if (batteryCount == 0)
                continue;

            int remainingCost = Mathf.Max(0, downstreamCost - genSupply);
            total += remainingCost / batteryCount;
        }

        return total;
    }

    private int GetGeneratorSupply(ElectricDevice device)
    {
        int supply = 0;
        if (device.inputPorts == null)
            return 0;

        foreach (Port p in device.inputPorts)
        {
            if (p == null || p.value <= 0 || p.connectedWire == null || p.connectedWire.outputPort == null)
                continue;

            PowerSource gen = p.connectedWire.outputPort.GetComponentInParent<PowerSource>();
            if (gen != null)
                supply += gen.currentOutput;
        }

        return supply;
    }

    private int GetActiveBatteryCount(ElectricDevice device)
    {
        int count = 0;
        if (device.inputPorts == null)
            return 0;

        foreach (Port p in device.inputPorts)
        {
            if (p == null || p.value <= 0 || p.connectedWire == null || p.connectedWire.outputPort == null)
                continue;

            PowerSource gen = p.connectedWire.outputPort.GetComponentInParent<PowerSource>();
            if (gen != null)
                continue;

            count++;
        }

        return count;
    }

    private List<Port> GetOutputPortsOf(EletricUnit unit)
    {
        if (unit is ElectricDevice device)
            return device.outputPorts ?? new List<Port>();

        if (unit is PowerStorage storage)
            return storage.outputPort != null ? new List<Port> { storage.outputPort } : new List<Port>();

        if (unit is PowerSource source)
            return source.outputPort != null ? new List<Port> { source.outputPort } : new List<Port>();

        return new List<Port>();
    }
    public override void OnDetected()
    {
        base.OnDetected();
        if (ElectricUI1.instance != null)
            ElectricUI1.instance.ShowStorageDataPanel(unitName, currentStorageAmount.ToString(), maxCapacity.ToString(), maxOutput.ToString());
    }
}
