using Mirror;
using UnityEngine;

public sealed class NetworkSwitch : NetworkBehaviour
{
    public Switch switchDevice;
    private const float MaxToggleDistance = 5f;

    [SyncVar(hook = nameof(OnSynchronizedStateChanged))]
    private bool synchronizedState;

    private void Awake()
    {
        if (switchDevice == null)
            switchDevice = GetComponent<Switch>();
    }

    private void Start()
    {
        if (switchDevice == null)
            return;

        if (isServer)
        {
            synchronizedState = switchDevice.switchState;
            switchDevice.SetNetworkState(synchronizedState);
        }
        else
        {
            switchDevice.SetNetworkState(synchronizedState);
        }
    }

    /// <summary>
    /// Requests a server-authoritative switch toggle.
    /// </summary>
    public void RequestToggle()
    {
        if (isServer)
        {
            ToggleOnServer();
            return;
        }

        if (isClient)
            CmdToggle();
    }

    [Command(requiresAuthority = false)]
    private void CmdToggle(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null)
            return;

        if (Vector3.Distance(sender.identity.transform.position, transform.position) > MaxToggleDistance)
            return;

        ToggleOnServer();
    }

    [Server]
    private void ToggleOnServer()
    {
        synchronizedState = !synchronizedState;
        if (switchDevice != null)
            switchDevice.SetNetworkState(synchronizedState);
    }

    private void OnSynchronizedStateChanged(bool _, bool newState)
    {
        if (switchDevice != null)
            switchDevice.SetNetworkState(newState);
    }
}
