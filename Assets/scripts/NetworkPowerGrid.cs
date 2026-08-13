using Mirror;
using UnityEngine;

public sealed class NetworkPowerGrid : NetworkBehaviour
{
    public static NetworkPowerGrid Instance { get; private set; }

    [SyncVar]
    public float availablePower;

    [SyncVar]
    public bool gridOnline;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    [ServerCallback]
    private void Update()
    {
        RecalculateServerState();
    }

    [Server]
    public void SetPower(float power)
    {
        availablePower = Mathf.Max(0f, power);
        gridOnline = availablePower > 0f;
        RpcPowerStateChanged(gridOnline, availablePower);
    }

    [Server]
    private void RecalculateServerState()
    {
        availablePower = Mathf.Max(0f, availablePower);
        gridOnline = availablePower > 0f;
    }

    [ClientRpc]
    private void RpcPowerStateChanged(bool online, float power)
    {
        gridOnline = online;
        availablePower = power;
    }
}
