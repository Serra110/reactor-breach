using Mirror;
using UnityEngine;

public abstract class NetworkMachine : NetworkBehaviour
{
    [SyncVar]
    public bool machineActive;

    [SyncVar]
    public float machineProgress;

    [ServerCallback]
    protected virtual void Update()
    {
        if (!machineActive)
            return;

        TickServer(Time.deltaTime);
    }

    protected abstract void TickServer(float deltaTime);

    [ClientRpc]
    protected void RpcSetMachineFeedback(bool active, float progress)
    {
        machineActive = active;
        machineProgress = progress;
    }

    [Server]
    protected void SetMachineState(bool active, float progress)
    {
        machineActive = active;
        machineProgress = Mathf.Clamp01(progress);
        RpcSetMachineFeedback(machineActive, machineProgress);
    }
}
