using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuotaHUD : MonoBehaviour
{
    private QuotaSystem quota;
    private Image borderImage;
    private Image panelImage;
    private Image iconImage;
    private Image fillImage;
    private Slider progressBar;
    private TMP_Text titleText;
    private TMP_Text countText;
    private TMP_Text timerText;
    private TMP_Text bannerText;
    private RectTransform pulseTransform;
    private RectTransform bannerTransform;

    private Color normalColor = new Color(0.07f, 0.10f, 0.14f, 0.96f);
    private Color urgentColor = new Color(0.42f, 0.10f, 0.05f, 0.97f);
    private Color completeColor = new Color(0.05f, 0.32f, 0.15f, 0.97f);
    private Color borderColor = new Color(0f, 0f, 0f, 0.6f);
    private Color fillNormal = new Color(0.16f, 0.62f, 1f, 1f);
    private Color fillUrgent = new Color(1f, 0.45f, 0.10f, 1f);
    private Color fillComplete = new Color(0.30f, 1f, 0.45f, 1f);
    private Color bannerAmber = new Color(1f, 0.82f, 0.30f, 1f);
    private Color bannerGreen = new Color(0.45f, 1f, 0.60f, 1f);
    private Color bannerRed = new Color(1f, 0.35f, 0.25f, 1f);

    private float bannerTimer;
    private QuotaState lastState = (QuotaState)(-1);
    private int lastCount = -1;
    private int lastRequired = -1;
    private int lastLevel = -1;
    private Sprite lastIcon;
    private float lastTimerSecond = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<QuotaHUD>() != null) return;
        GameObject hudObject = new GameObject("Quota HUD");
        hudObject.AddComponent<QuotaHUD>();
    }

    private void Start()
    {
        quota = QuotaSystem.Instance != null ? QuotaSystem.Instance : FindFirstObjectByType<QuotaSystem>();
        BuildInterface();
        if (quota != null)
        {
            quota.OnQuotaStarted += HandleStarted;
            quota.OnQuotaProgressChanged += HandleProgress;
            quota.OnQuotaCompleted += HandleCompleted;
            quota.OnQuotaFailed += HandleFailed;
            quota.OnStateChanged += HandleStateChanged;
            Refresh();
            ShowBanner($"DAY {quota.Level}: PRODUCE {quota.RequiredAmount} {quota.TargetName.ToUpperInvariant()}", bannerAmber);
        }
    }

    private void Update()
    {
        if (quota == null) return;
        Refresh();
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(bannerTimer / 0.35f);
            Color color = bannerText.color;
            color.a = Mathf.Clamp01(bannerTimer < 0.35f ? fade : 1f);
            bannerText.color = color;
            float settle = Mathf.Clamp01((3.5f - bannerTimer) / 0.35f);
            if (bannerTransform != null)
                bannerTransform.localScale = Vector3.one * (1f - (1f - settle) * 0.06f);
            if (bannerTimer <= 0f) bannerText.gameObject.SetActive(false);
        }

        if (quota.State == QuotaState.Urgent && pulseTransform != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.02f;
            pulseTransform.localScale = new Vector3(pulse, pulse, 1f);
        }
        else if (pulseTransform != null)
        {
            pulseTransform.localScale = Vector3.one;
        }
    }

    private void OnDestroy()
    {
        if (quota == null) return;
        quota.OnQuotaStarted -= HandleStarted;
        quota.OnQuotaProgressChanged -= HandleProgress;
        quota.OnQuotaCompleted -= HandleCompleted;
        quota.OnQuotaFailed -= HandleFailed;
        quota.OnStateChanged -= HandleStateChanged;
    }

    private void BuildInterface()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject outer = CreateUIObject("Quota Panel", transform);
        RectTransform outerRect = outer.GetComponent<RectTransform>();
        outerRect.anchorMin = new Vector2(0f, 1f);
        outerRect.anchorMax = new Vector2(0f, 1f);
        outerRect.pivot = new Vector2(0f, 1f);
        outerRect.anchoredPosition = new Vector2(32f, -32f);
        outerRect.sizeDelta = new Vector2(470f, 132f);
        borderImage = outer.AddComponent<Image>();
        borderImage.color = borderColor;
        pulseTransform = outerRect;

        GameObject inner = CreateUIObject("Quota Background", outer.transform);
        RectTransform innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);
        panelImage = inner.AddComponent<Image>();
        panelImage.color = normalColor;

        GameObject icon = CreateUIObject("Quota Icon", inner.transform);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(18f, 0f);
        iconRect.sizeDelta = new Vector2(76f, 76f);
        iconImage = icon.AddComponent<Image>();
        iconImage.preserveAspect = true;

        titleText = CreateText("Quota Title", inner.transform, new Vector2(112f, -16f), new Vector2(330f, 30f), 22f, TextAlignmentOptions.Left);
        countText = CreateText("Quota Count", inner.transform, new Vector2(112f, -52f), new Vector2(330f, 27f), 19f, TextAlignmentOptions.Left);
        timerText = CreateText("Quota Timer", inner.transform, new Vector2(330f, -16f), new Vector2(125f, 30f), 19f, TextAlignmentOptions.Right);

        GameObject barObject = CreateUIObject("Quota Progress Bar", inner.transform);
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.offsetMin = new Vector2(112f, 18f);
        barRect.offsetMax = new Vector2(-16f, 42f);
        progressBar = barObject.AddComponent<Slider>();
        progressBar.minValue = 0f;
        progressBar.maxValue = 1f;
        progressBar.interactable = false;
        Image barBackground = barObject.AddComponent<Image>();
        barBackground.color = new Color(0f, 0f, 0f, 0.35f);
        GameObject fill = CreateUIObject("Fill", barObject.transform);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = fillNormal;
        progressBar.fillRect = fill.GetComponent<RectTransform>();
        progressBar.fillRect.anchorMin = Vector2.zero;
        progressBar.fillRect.anchorMax = Vector2.one;
        progressBar.fillRect.offsetMin = Vector2.zero;
        progressBar.fillRect.offsetMax = Vector2.zero;
        progressBar.direction = Slider.Direction.LeftToRight;

        bannerText = CreateText("Quota Banner", transform, new Vector2(0f, -200f), new Vector2(820f, 70f), 34f, TextAlignmentOptions.Center);
        bannerText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        bannerText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        bannerText.rectTransform.pivot = new Vector2(0.5f, 1f);
        bannerText.gameObject.SetActive(false);
        bannerTransform = bannerText.rectTransform;
    }

    private TMP_Text CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject created = new GameObject(objectName, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created;
    }

    private void Refresh()
    {
        if (quota == null || titleText == null) return;

        if (quota.Level != lastLevel)
        {
            titleText.text = $"QUOTA // DAY {quota.Level}";
            lastLevel = quota.Level;
        }

        int current = quota.CurrentAmount;
        int required = quota.RequiredAmount;
        if (current != lastCount || required != lastRequired)
        {
            countText.text = $"{quota.TargetName}: {current} / {required}";
            lastCount = current;
            lastRequired = required;
        }

        if (quota.targetItem != null && quota.targetItem.Icon != lastIcon)
        {
            iconImage.sprite = quota.targetItem.Icon;
            lastIcon = quota.targetItem.Icon;
        }

        if (quota.useTimer)
        {
            int second = Mathf.CeilToInt(quota.TimeRemaining);
            if (second != lastTimerSecond)
            {
                timerText.text = FormatTime(quota.TimeRemaining);
                lastTimerSecond = second;
            }
            timerText.color = second <= 30f ? bannerRed : Color.white;
        }
        else if (!timerText.text.Equals("NO TIME LIMIT"))
        {
            timerText.text = "NO TIME LIMIT";
            timerText.color = Color.white;
        }

        float target = quota.Progress01;
        progressBar.value = Mathf.MoveTowards(progressBar.value, target, Time.unscaledDeltaTime * 2.5f);

        if (quota.State != lastState)
        {
            lastState = quota.State;
            ApplyStateColor(lastState);
        }
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private void ApplyStateColor(QuotaState state)
    {
        panelImage.color = state == QuotaState.Urgent ? urgentColor : state == QuotaState.Complete ? completeColor : normalColor;
        if (fillImage != null)
            fillImage.color = state == QuotaState.Urgent ? fillUrgent : state == QuotaState.Complete ? fillComplete : fillNormal;
    }

    private void HandleStarted(int level, int amount) => ShowBanner($"DAY {level}: PRODUCE {amount} {quota.TargetName.ToUpperInvariant()}", bannerAmber);
    private void HandleProgress(int current, int required) { }
    private void HandleCompleted(int completedLevel) => ShowBanner($"QUOTA COMPLETE! DAY {completedLevel}", bannerGreen);
    private void HandleFailed(int failedLevel) => ShowBanner($"QUOTA FAILED // DAY {failedLevel}", bannerRed);
    private void HandleStateChanged(QuotaState nextState)
    {
        if (lastState != nextState)
        {
            lastState = nextState;
            ApplyStateColor(nextState);
        }
    }

    private void ShowBanner(string message, Color color)
    {
        if (bannerText == null) return;
        bannerText.text = message;
        bannerText.color = color;
        bannerText.gameObject.SetActive(true);
        if (bannerTransform != null) bannerTransform.localScale = Vector3.one * 1.06f;
        bannerTimer = 3.5f;
    }
}
