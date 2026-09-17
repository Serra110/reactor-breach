using UnityEngine;

/// <summary>
/// Flags centrais de debug. Mete a true apenas em desenvolvimento.
/// No build de MVP, todos os logs de producao sao suprimidos.
/// </summary>
public static class DebugFlags
{
    [Tooltip("Logs de UI (hover, drag, drop). Spammy em dev.")]
    public static bool uiLogs = false;

    [Tooltip("Logs de construcao (placement/destruicao).")]
    public static bool buildLogs = false;

    [Tooltip("Logs de inventario.")]
    public static bool inventoryLogs = false;

    [Tooltip("Logs do sistema eletrico (PowerSource/PowerStorage).")]
    public static bool electricLogs = false;
}