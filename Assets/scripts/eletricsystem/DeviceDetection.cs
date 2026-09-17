using UnityEngine;

public class DeviceDetector : MonoBehaviour
{
    public EletricUnit detectedUnit;
    public float detectingRange = 5f;
    public Camera playerCamera;

    private SignalLever draggedLever;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private bool cursorStateStored;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
    }

    private Camera GetViewCamera()
    {
        if (playerCamera != null && playerCamera.isActiveAndEnabled)
            return playerCamera;
        return Camera.main;
    }

    private void Update()
    {
        HandleDetectDevices();
        HandleDeviceEnteractions();
        HandleLeverDragging();
        HandleButtonClick();
    }

    /// <summary>
    /// Handles the existing E-key interaction for the currently detected device.
    /// </summary>
    public void HandleDeviceEnteractions()
    {
        if (detectedUnit == null || !Input.GetKeyDown(KeyCode.E))
            return;

        detectedUnit.OnEnteract();
    }
    /// <summary>
    /// Presses a detected signal button with the primary mouse button.
    /// </summary>
    private void HandleButtonClick()
    {
        if (draggedLever != null || !Input.GetMouseButtonDown(0))
            return;

        SignalButton button = detectedUnit as SignalButton;
        if (button != null)
            button.Press();
    }



    private void HandleLeverDragging()
    {
        SignalLever aimedLever = detectedUnit as SignalLever;

        if (draggedLever != null && draggedLever != aimedLever)
        {
            EndLeverDrag();
            return;
        }

        if (draggedLever == null && aimedLever != null && Input.GetMouseButtonDown(0))
        {
            draggedLever = aimedLever;
            draggedLever.BeginDrag();
            CaptureCursorForLeverDrag();
        }

        if (draggedLever == null)
            return;

        if (Input.GetMouseButton(0))
            draggedLever.UpdateDragFromMousePosition(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            EndLeverDrag();
    }

    private float GetMouseVerticalDelta()
    {
        float inputDelta = Input.GetAxisRaw("Mouse Y");
        if (Mathf.Abs(inputDelta) > 0.01f)
            return inputDelta;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
            return UnityEngine.InputSystem.Mouse.current.delta.ReadValue().y;
#endif

        return 0f;
    }

    private void CaptureCursorForLeverDrag()
    {
        if (!cursorStateStored)
        {
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            cursorStateStored = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void EndLeverDrag()
    {
        if (draggedLever != null)
            draggedLever.EndDrag();

        draggedLever = null;
        RestoreCursorAfterLeverDrag();
    }

    private void RestoreCursorAfterLeverDrag()
    {
        if (!cursorStateStored)
            return;

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;
        cursorStateStored = false;
    }

    /// <summary>
    /// Finds the device currently under the player's crosshair.
    /// </summary>
    public void HandleDetectDevices()
    {
        if (draggedLever != null)
            return;

        Camera viewCamera = GetViewCamera();
        if (viewCamera == null)
            return;

        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, detectingRange))
        {
            ReactorController reactor = hit.transform.GetComponentInParent<ReactorController>();
            EletricUnit unit = reactor;

            if (unit == null)
                unit = hit.transform.GetComponent<EletricUnit>();
            if (unit == null)
            {
                DeviceNameLink link = hit.transform.GetComponent<DeviceNameLink>();
                if (link != null)
                    unit = link.linkedUnit;
            }
            if (unit == null)
                unit = hit.transform.GetComponentInParent<EletricUnit>();
            if (unit == null)
                unit = hit.transform.GetComponentInChildren<EletricUnit>();

            if (unit != null)
            {
                if (detectedUnit == unit)
                {
                    if (unit is Port port && ElectricUI1.instance != null)
                        ElectricUI1.instance.UpdatePortValue(port.displayValue);
                    return;
                }

                detectedUnit = unit;

                if (ElectricUI1.instance != null)
                {
                    if (unit is ReactorController detectedReactor)
                        ElectricUI1.instance.ShowDeviceName(detectedReactor.unitName);
                    else if (unit is Port detectedPort)
                        ElectricUI1.instance.ShowPortData(detectedPort.displayName, detectedPort.displayValue);
                    else
                        ElectricUI1.instance.ShowDeviceName(unit.unitName);
                }

                detectedUnit.OnDetected();
            }
            else
            {
                ClearDetectedUnit();
            }
        }
        else
        {
            ClearDetectedUnit();
        }
    }

    private void ClearDetectedUnit()
    {
        if (draggedLever != null)
            EndLeverDrag();

        if (detectedUnit == null)
            return;

        detectedUnit = null;
        if (ElectricUI1.instance != null)
            ElectricUI1.instance.HideAll();
    }

    private void OnDisable()
    {
        EndLeverDrag();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && draggedLever != null)
            EndLeverDrag();
    }
}
