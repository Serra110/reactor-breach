using UnityEngine;

public enum NodeSignal
{
    Command,
    Telemetry
}

public class Node : Port
{
    [Tooltip("Logical label for this node, such as Command, Heat, or Fuel.")]
    public string nodeLabel = "Node";

    [Tooltip("Signal category expected by this node.")]
    public NodeSignal signalType = NodeSignal.Command;

    public override string displayName => string.IsNullOrWhiteSpace(nodeLabel) ? name : nodeLabel;

    /// <summary>
    /// Returns the useful signal value instead of the packed ReactorPacket integer.
    /// Continuous commands retain their 0-100 value, while trigger commands are binary.
    /// </summary>
    public override int displayValue
    {
        get
        {
            int action = ReactorPacket.GetAction(value);
            int packetValue = ReactorPacket.GetValue(value);

            if (action == ReactorPacket.ActionTrigger)
                return packetValue > 0 ? 1 : 0;

            if (action == ReactorPacket.ActionEnable)
                return 1;

            if (action == ReactorPacket.ActionDisable)
                return 0;

            return packetValue;
        }
    }
}
