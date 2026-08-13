using UnityEngine;
using UnityEngine.UI;

public class ResponsiveCanvasSetup : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField, Range(0f, 1f)] private float matchWidthHeight = 0.5f;

    private void Awake()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthHeight;
            scaler.referencePixelsPerUnit = 100f;
        }
    }
}
