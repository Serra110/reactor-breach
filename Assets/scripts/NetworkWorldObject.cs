using Mirror;
using UnityEngine;

public class NetworkWorldObject : NetworkBehaviour
{
    [SyncVar]
    public bool worldObjectActive = true;

    [SyncVar]
    public int resourceAmount;

    public override void OnStartServer()
    {
        base.OnStartServer();
        SyncInitialState();
    }

    public void SetWorldObjectState(bool active, int amount)
    {
        worldObjectActive = active;
        resourceAmount = Mathf.Max(0, amount);
        if (NetworkServer.active)
            RpcApplyWorldObjectState(worldObjectActive, resourceAmount);
    }

    [ClientRpc]
    private void RpcApplyWorldObjectState(bool active, int amount)
    {
        worldObjectActive = active;
        resourceAmount = amount;
        gameObject.SetActive(active);
    }

    protected virtual void SyncInitialState()
    {
        resourceAmount = Mathf.Max(0, resourceAmount);
    }
}
