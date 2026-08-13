using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeviceDetector : MonoBehaviour
{
    public EletricUnit detectedUnit;
    public float detectingRange = 5;
    public Camera playerCamera;

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

    // Update is called once per frame
    void Update()
    {
        HandleDetectDevices();
        HandleDeviceEnteractions();
    }

    public void HandleDeviceEnteractions() 
    {
        if (detectedUnit == null) { return; }

        if (Input.GetKeyDown(KeyCode.E)) 
        {
            detectedUnit.OnEnteract();
        }
    }

    public void HandleDetectDevices() 
    {
        Camera viewCamera = GetViewCamera();
        if (viewCamera == null)
            return;

        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, detectingRange)) 
        {
            EletricUnit unit = hit.transform.GetComponent<EletricUnit>();
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
                        ElectricUI1.instance.UpdatePortValue(port.value);
                    return; 
                }

                detectedUnit = unit;

                if (ElectricUI1.instance != null)
                {
                    if (unit is Port detectedPort)
                        ElectricUI1.instance.ShowPortData(detectedPort.name, detectedPort.value);
                    else
                        ElectricUI1.instance.ShowDeviceName(unit.unitName);
                }

                detectedUnit.OnDetected();
            }
            else
            {
                if (detectedUnit != null)
                {
                    detectedUnit = null;
                    if (ElectricUI1.instance != null)
                        ElectricUI1.instance.HideAll();
                }
            }
        }
        else
        {
            if (detectedUnit != null)
            {
                detectedUnit = null;
                if (ElectricUI1.instance != null)
                    ElectricUI1.instance.HideAll();
            }
        } 
   }

}
