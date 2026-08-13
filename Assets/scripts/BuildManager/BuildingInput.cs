using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SimpleBuildingSystem
{
    // Thin input layer using Unity's legacy Input Manager for simplicity.
    // Other system components do not depend on how input is read.
    [RequireComponent(typeof(BuildingController))]
    public class BuildingInput : MonoBehaviour
    {
        [Header("Trocar de modo")]
    public KeyCode placementModeKey = KeyCode.B;
    public KeyCode destructionModeKey = KeyCode.X;
    public KeyCode idleModeKey = KeyCode.Escape;

        [Header("Actions within a mode")]
    public KeyCode confirmKey = KeyCode.Mouse0;
    public KeyCode cancelKey = KeyCode.Mouse1;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

        private BuildingController _controller;
        private float _diagTimer;

        private void Awake()
        {
            _controller = GetComponent<BuildingController>();
        }

        private void Update()
        {
            _diagTimer += Time.unscaledDeltaTime;
            if (_diagTimer > 1f)
            {
                _diagTimer = 0f;
#if ENABLE_INPUT_SYSTEM
                Vector2 newMouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                Vector2 newMouse = Vector2.zero;
#endif
                Debug.Log($"[BuildInput] ALIVE screen={Screen.width}x{Screen.height} legacyMouse={Input.mousePosition} newMouse={newMouse}");
            }

            bool bPressed = Input.GetKeyDown(placementModeKey);
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current[Key.B].wasPressedThisFrame)
                bPressed = true;
#endif
            if (bPressed)
            {
                Debug.Log("[BuildInput] B pressed -> TogglePlacementMode");
                _controller.TogglePlacementMode();
            }
            if (Input.GetKeyDown(destructionModeKey)) _controller.RequestDestructionMode();
            if (Input.GetKeyDown(idleModeKey)) _controller.SwitchToIdle();

            if (Input.GetKeyDown(confirmKey)) _controller.CurrentState?.OnConfirm();
            if (Input.GetKeyDown(cancelKey)) _controller.CurrentState?.OnCancel();

            if (Input.GetKeyDown(rotateLeftKey)) _controller.CurrentState?.OnRotate(-1f);
            if (Input.GetKeyDown(rotateRightKey)) _controller.CurrentState?.OnRotate(1f);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 newScroll = Mouse.current.scroll.ReadValue();
                if (newScroll.y > 0f)
                    scroll = 1f;
                else if (newScroll.y < 0f)
                    scroll = -1f;
            }
#endif
            if (scroll != 0f)
                Debug.Log($"[BuildInput] SCROLL value={scroll}");
            if (scroll > 0f)
                _controller.SelectNextBuildItem(1);
            else if (scroll < 0f)
                _controller.SelectNextBuildItem(-1);
        }
    }
}
