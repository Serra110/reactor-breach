/// <summary>
/// Pacote que viaja nos fios (bus). Um único int com:
///   bits 24-31 = Target (para onde vai o comando)
///   bits 16-23 = Action (o que fazer)
///   bits  0-15 = Value  (valor 0-65535, normalmente 0-100)
/// Reactor envia telemetria com Target=TargetTelemetry e Action=canal.
/// </summary>
public static class ReactorPacket
{
    public const int MaskTarget = unchecked((int)0xFF000000);
    public const int MaskAction = unchecked((int)0x00FF0000);
    public const int MaskValue  = 0x0000FFFF;

    // ===== Targets (para onde) =====
    public const int TargetReactor   = 1;
    public const int TargetRods      = 2;
    public const int TargetCoolant   = 3;
    public const int TargetFeedwater = 4;
    public const int TargetTurbine   = 5;
    public const int TargetBreaker   = 6;
    public const int TargetTelemetry = 100;

    // ===== Actions (o que) =====
    public const int ActionSet      = 1;
    public const int ActionEnable   = 2;
    public const int ActionDisable  = 3;
    public const int ActionToggle   = 4;
    public const int ActionTrigger  = 5;
    public const int ActionIncrease = 6;
    public const int ActionDecrease = 7;

    // ===== Triggers do reactor (ActionTrigger + valor) =====
    public const int TriggerStart = 1;
    public const int TriggerStop  = 2;
    public const int TriggerScram = 3;

    public static int Make(int target, int action, int value = 0)
    {
        return (target << 24) | (action << 16) | (value & MaskValue);
    }

    public static int GetTarget(int packet) => (packet & MaskTarget) >> 24;
    public static int GetAction(int packet) => (packet & MaskAction) >> 16;
    public static int GetValue(int packet)  => packet & MaskValue;

    public static string GetTargetName(int target)
    {
        switch (target)
        {
            case TargetReactor: return "REACTOR";
            case TargetRods: return "RODS";
            case TargetCoolant: return "COOLANT";
            case TargetFeedwater: return "FEEDWATER";
            case TargetTurbine: return "TURBINE";
            case TargetBreaker: return "BREAKER";
            case TargetTelemetry: return "TELEMETRY";
            default: return "T" + target;
        }
    }

    public static string GetActionName(int action)
    {
        switch (action)
        {
            case ActionSet: return "SET";
            case ActionEnable: return "ENABLE";
            case ActionDisable: return "DISABLE";
            case ActionToggle: return "TOGGLE";
            case ActionTrigger: return "TRIGGER";
            case ActionIncrease: return "INCREASE";
            case ActionDecrease: return "DECREASE";
            default: return "A" + action;
        }
    }

    public static string Describe(int packet)
    {
        return GetTargetName(GetTarget(packet)) + " " + GetActionName(GetAction(packet)) + " " + GetValue(packet);
    }
}
