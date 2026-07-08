using UnityEngine;

namespace SimpleBuildingSystem
{
    // Camada fina sobre o Input do Unity, usando o Input Manager "legacy" por simplicidade.
    // Se mais tarde migrares para o New Input System, só precisas de alterar este ficheiro —
    // nenhum outro componente do sistema depende de como o input é lido.
    [RequireComponent(typeof(BuildingController))]
    public class BuildingInput : MonoBehaviour
    {
        [Header("Trocar de modo")]
    public KeyCode placementModeKey = KeyCode.B;
    public KeyCode destructionModeKey = KeyCode.Alpha2;
    public KeyCode idleModeKey = KeyCode.Escape;

    [Header("Ações dentro de um modo")]
    public KeyCode confirmKey = KeyCode.Mouse0;
    public KeyCode cancelKey = KeyCode.Mouse1;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

        private BuildingController _controller;

        private void Awake()
        {
            _controller = GetComponent<BuildingController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(placementModeKey)) _controller.TogglePlacementMode();
            if (Input.GetKeyDown(destructionModeKey)) _controller.RequestDestructionMode();
            if (Input.GetKeyDown(idleModeKey)) _controller.SwitchToIdle();

            if (Input.GetKeyDown(confirmKey)) _controller.CurrentState?.OnConfirm();
            if (Input.GetKeyDown(cancelKey)) _controller.CurrentState?.OnCancel();

            if (Input.GetKeyDown(rotateLeftKey)) _controller.CurrentState?.OnRotate(-1f);
            if (Input.GetKeyDown(rotateRightKey)) _controller.CurrentState?.OnRotate(1f);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
                _controller.SelectNextBuildItem(1);
            else if (scroll < 0f)
                _controller.SelectNextBuildItem(-1);
        }
    }
}
