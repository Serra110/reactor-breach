using UnityEngine;

/// <summary>
/// Base de todos os módulos de sinal. Sendo um SignalDevice, tem
/// inputNodes/outputNodes configuráveis. O DeviceDetector do jogador
/// já o deteta e chama OnEnteract() quando se prime E.
/// </summary>
public class SignalModule : SignalDevice
{
    [Header("UI")]
    [Tooltip("Painel opcional que abre/fecha ao interagir (alavanca/slider, toggle...).")]
    public GameObject controlUI;

    public override void OnEnteract()
    {
        if (controlUI == null) return;

        bool open = !controlUI.activeInHierarchy;
        controlUI.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    protected static void ForceOutputType(Node node)
    {
        if (node != null) node.type = PortType.Output;
    }

    protected static void ForceInputType(Node node)
    {
        if (node != null) node.type = PortType.Input;
    }
}
