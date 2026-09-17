using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeonLight : ElectricDevice
{

    public GameObject lightObject;
    private Renderer lightRenderer;
    public new Light light;
    public Color lightColor;

    public Material emissionOnMaterial;
    public Material emissionOffMaterial;

    public override void Start()
    {
        base.Start();

        if (lightObject != null)
            lightRenderer = lightObject.GetComponent<Renderer>();

        ApplyState(false);
    }

    public override void OnDevicePowerStateChanged(bool newState)
    {
        base.OnDevicePowerStateChanged(newState);

        ApplyState(newState);
    }

    private void ApplyState(bool powered)
    {
        if (lightRenderer == null || lightRenderer.material == null)
            return;

        if (light != null)
            light.gameObject.SetActive(powered);

        if (powered)
        {
            if (light != null)
                light.color = lightColor;

            if (emissionOnMaterial != null)
                lightRenderer.material = emissionOnMaterial;
            else
            {
                lightRenderer.material.EnableKeyword("_EMISSION");
                lightRenderer.material.SetColor("_EmissionColor", lightColor);
            }
        }
        else
        {
            if (emissionOffMaterial != null)
                lightRenderer.material = emissionOffMaterial;
            else
            {
                lightRenderer.material.EnableKeyword("_EMISSION");
                lightRenderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
