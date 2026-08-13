using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public enum PortType
{
    Input,
    Output
}
public class Port : EletricUnit
{

    public int value;
    public System.Action<int> onValueChanged;
    public PortType type;
    public bool isConnected;
    public Wire connectedWire;
    
    public void SetValue(int newValue)
    {
        if (newValue != value)
        {
            value = newValue;
            onValueChanged?.Invoke(value);
        }
    }
    void Start()
    {
        rend = GetComponent<Renderer>();
        ResetPort();
    }

    public void ResetPort()
    {
        value = 0;
        isConnected = false;
        connectedWire = null;

        if (portTween != null)
        {
            portTween.Kill();
            portTween = null;
        }

        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        if (rend == null) return;
        if (cachedMaterial == null)
            cachedMaterial = rend.material;
        cachedMaterial.color = Color.white;
    }
    public Color ShowColor;
    public Color HideColor;
    public Renderer rend;

    private Material cachedMaterial;
    Tween portTween;
    public void ShowPort()
    {
        if (rend == null) return;
        if (cachedMaterial == null)
            cachedMaterial = rend.material;
        if (portTween != null)
        {
            portTween.Kill();
        }
        cachedMaterial.color = ShowColor;
        portTween = transform.DOScale(new Vector3(0.2f, 0.2f, 0.2f), 0.4f);
    }
    public void HidePort()
    {
        if (rend == null) return;
        if (cachedMaterial == null)
            cachedMaterial = rend.material;
        if (portTween != null)
        {
            portTween.Kill();
        }
        cachedMaterial.color = HideColor;
        portTween = transform.DOScale(new Vector3(0.1f, 0.1f, 0.1f), 0.4f);
    }
}
