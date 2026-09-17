/// <summary>
/// Estados operacionais do reactor (por ordem de severidade).
/// </summary>
public static class ReactorStatus
{
    public const int Offline  = 0;
    public const int Starting = 1;
    public const int Running  = 2;
    public const int Warning  = 3;
    public const int Critical = 4;
    public const int Scram    = 5;
    public const int Shutdown = 6;
    public const int Meltdown = 7;

    public static string GetName(int state)
    {
        switch (state)
        {
            case Offline: return "OFFLINE";
            case Starting: return "STARTING";
            case Running: return "RUNNING";
            case Warning: return "WARNING";
            case Critical: return "CRITICAL";
            case Scram: return "SCRAM";
            case Shutdown: return "SHUTDOWN";
            case Meltdown: return "MELTDOWN";
            default: return "?";
        }
    }
}
