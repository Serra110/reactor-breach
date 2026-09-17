using Mirror;
using UnityEngine;

public sealed class NetworkPlayerSetup : NetworkBehaviour
{
    private PlayerMovement legacyMovement;
    private PlayerInteraction interaction;
    private DeviceDetector deviceDetector;
    private CameraFollower cameraFollower;
    private Camera playerCamera;
    private AudioListener audioListener;
    private SimpleBuildingSystem.BuildingInput buildingInput;
    private SimpleBuildingSystem.BuildingController buildingController;

    private void Awake()
    {
        legacyMovement = GetComponent<PlayerMovement>();
        interaction = GetComponent<PlayerInteraction>();
        deviceDetector = GetComponent<DeviceDetector>();
        cameraFollower = GetComponentInChildren<CameraFollower>(true);
        playerCamera = GetComponentInChildren<Camera>(true);
        audioListener = GetComponentInChildren<AudioListener>(true);
        buildingInput = GetComponentInChildren<SimpleBuildingSystem.BuildingInput>(true);
        buildingController = GetComponentInChildren<SimpleBuildingSystem.BuildingController>(true);
        SetOwnerOnlyComponents(false);
    }

    public override void OnStartAuthority()
    {
        SetOwnerOnlyComponents(true);
    }

    public override void OnStartLocalPlayer()
    {
        SetOwnerOnlyComponents(true);
    }

    public override void OnStopAuthority()
    {
        SetOwnerOnlyComponents(false);
    }

    private void SetOwnerOnlyComponents(bool enabledForOwner)
    {
        bool offlineMode = !NetworkClient.active && !NetworkServer.active;

        if (legacyMovement != null)
            legacyMovement.enabled = offlineMode;
        if (interaction != null)
            interaction.enabled = offlineMode;
        if (deviceDetector != null)
            deviceDetector.enabled = enabledForOwner || offlineMode;
        if (cameraFollower != null)
            cameraFollower.enabled = enabledForOwner || offlineMode;
        if (playerCamera != null)
            playerCamera.enabled = enabledForOwner || offlineMode;
        if (audioListener != null)
            audioListener.enabled = enabledForOwner || offlineMode;
        if (buildingInput != null)
            buildingInput.enabled = enabledForOwner || offlineMode;
        if (buildingController != null)
            buildingController.enabled = enabledForOwner || offlineMode;

        if (enabledForOwner || offlineMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
