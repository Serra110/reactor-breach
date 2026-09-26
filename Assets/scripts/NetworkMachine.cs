using Mirror;
using UnityEngine;

public abstract class NetworkMachine : NetworkBehaviour
{
    [SyncVar]
    public bool machineActive;

    [SyncVar]
    public float machineProgress;

    protected virtual void Update()
    {
        if (!NetworkServer.active && !OfflineMvpBootstrap.IsOffline)
            return;

        if (!machineActive)
            return;

        TickServer(Time.deltaTime);
    }

    protected abstract void TickServer(float deltaTime);

    protected void RpcSetMachineFeedback(bool active, float progress)
    {
        machineActive = active;
        machineProgress = progress;
    }

    protected void SetMachineState(bool active, float progress)
    {
        machineActive = active;
        machineProgress = Mathf.Clamp01(progress);
        if (NetworkServer.active)
            RpcSetMachineFeedback(machineActive, machineProgress);
    }
}
