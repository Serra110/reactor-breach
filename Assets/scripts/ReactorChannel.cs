/// <summary>
/// Canais de telemetria emitidos pelo reactor no bus de saída (Node/Wire).
/// Pacote: Target=TargetTelemetry, Action=canal, Value=0-100.
///
/// IDs — este é o sistema legacy de telemetria
/// usado por SignalDisplay, SignalAlarm, SignalTranslator.
/// </summary>
public static class ReactorChannel
{
    public const int Temperature   = 1;  
    public const int CoolantLevel  = 2; 
    public const int Pressure      = 3;
    public const int PowerOutput   = 4;
    public const int Status        = 5;
    public const int RodInsertion  = 6;
    public const int NeutronFlux   = 7;
    public const int TurbineRPM    = 8;
    public const int Radiation     = 9;
    public const int EnergyTotal   = 10;
    public const int FuelPercent   = 11;

    public const int TelemetryCount = 11;

    public static string GetName(int channel)
    {
        switch (channel)
        {
            case Temperature:  return "TEMP";
            case CoolantLevel: return "COOLANT";
            case Pressure:     return "PRESS";
            case PowerOutput:  return "POWER";
            case Status:       return "STATUS";
            case RodInsertion: return "RODS";
            case NeutronFlux:  return "FLUX";
            case TurbineRPM:   return "TRPM";
            case Radiation:    return "RAD";
            case EnergyTotal:  return "MWh";
            case FuelPercent:  return "FUEL";
            default:           return "CH" + channel;
        }
    }

    public static string Format(int channel, int value)
    {
        switch (channel)
        {
            case Status:
                if (value >= 90) return "MELTDOWN";
                if (value >= 70) return "SCRAM";
                if (value >= 45) return "CRITICAL";
                if (value >= 20) return "WARNING";
                return "NORMAL";
            case Radiation:
                return value + " mSv";
            case EnergyTotal:
                return value + " MWh";
            default:
                return value + " %";
        }
    }
}
