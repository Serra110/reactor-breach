using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum PortType
{
    Input,
    Output
}

public class Port : EletricUnit
{
    public int value;
    public Action<int> onValueChanged;
    public PortType type;
    public bool isConnected;
    public Wire connectedWire;

    [SerializeField]
    private List<Wire> connectedWires = new List<Wire>();

    public virtual int displayValue => value;
    public virtual string displayName => string.IsNullOrWhiteSpace(unitName) ? name : unitName;

    /// <summary>
    /// Returns true for signal inputs that can receive more than one wire.
    /// Electrical ports keep the original one-wire behaviour.
    /// </summary>
    public bool AllowsMultipleConnections => this is Node && type == PortType.Input;

    /// <summary>
    /// Returns whether another wire may be connected to this port.
    /// </summary>
    public bool CanAcceptConnection(Wire candidate)
    {
        return !isConnected || AllowsMultipleConnections || (candidate != null && connectedWires.Contains(candidate));
    }

    /// <summary>
    /// Registers a wire while preserving the original connectedWire compatibility field.
    /// </summary>
    public void RegisterConnection(Wire wire)
    {
        if (wire == null)
            return;

        if (!connectedWires.Contains(wire))
            connectedWires.Add(wire);

        connectedWire = connectedWires[0];
        isConnected = true;
    }

    /// <summary>
    /// Removes a wire and updates the connection state.
    /// </summary>
    public void UnregisterConnection(Wire wire)
    {
        if (wire != null)
            connectedWires.Remove(wire);

        if (connectedWires.Count > 0)
        {
            connectedWire = connectedWires[0];
            isConnected = true;
        }
        else
        {
            connectedWire = null;
            isConnected = false;
        }
    }

    /// <summary>
    /// Changes the stored value and notifies listeners only when the value changed.
    /// </summary>
    public void SetValue(int newValue)
    {
        if (newValue != value)
        {
            value = newValue;
            onValueChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// Publishes a value even when it matches the previous value.
    /// </summary>
    public void PublishValue(int newValue)
    {
        value = newValue;
        onValueChanged?.Invoke(value);
    }

    /// <summary>
    /// Receives a wired value and always notifies listeners, including repeated pulses.
    /// </summary>
    public void ReceiveValue(int newValue)
    {
        value = newValue;
        onValueChanged?.Invoke(value);
    }

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        baseScale = transform.localScale;
        rend = GetComponent<Renderer>();
    }

    public void SetBaseScale(float scale)
    {
        baseScale = Vector3.one * scale;
        transform.localScale = baseScale;
    }

    private void Start()
    {
        ResetPort();
    }

    public void ResetPort()
    {
        value = 0;
        isConnected = false;
        connectedWire = null;
        connectedWires.Clear();

        if (portTween != null)
        {
            portTween.Kill();
            portTween = null;
        }

        transform.localScale = baseScale;

        if (rend == null)
            return;

        if (cachedMaterial == null)
            cachedMaterial = rend.material;

        cachedMaterial.color = Color.white;
    }

    public Color ShowColor;
    public Color HideColor;
    public Renderer rend;

    private Material cachedMaterial;
    private Tween portTween;

    public void ShowPort()
    {
        if (rend == null)
            return;

        if (cachedMaterial == null)
            cachedMaterial = rend.material;

        if (portTween != null)
            portTween.Kill();

        cachedMaterial.color = ShowColor;
        portTween = transform.DOScale(baseScale * 1.75f, 0.4f);
    }

    public void HidePort()
    {
        if (rend == null)
            return;

        if (cachedMaterial == null)
            cachedMaterial = rend.material;

        if (portTween != null)
            portTween.Kill();

        cachedMaterial.color = HideColor;
        portTween = transform.DOScale(baseScale, 0.4f);
    }
}
