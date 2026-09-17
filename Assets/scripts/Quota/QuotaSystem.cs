using System;
using System.Collections.Generic;
using ReactorBreach.InventorySystem;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum QuotaProgressMode
{
    Inventory,
    ProductionCounter,
    InventoryOrProduction
}

public enum QuotaState
{
    Normal,
    Urgent,
    Complete,
    Failed
}

public class QuotaSystem : MonoBehaviour
{
    public static QuotaSystem Instance { get; private set; }

    [Header("Objective")]
    public ItemSO targetItem;
    public string targetItemName = "IronIngot";
    [Min(1)] public int baseQuantity = 20;
    [Min(0)] public int quantityIncreasePerLevel = 5;
    public QuotaProgressMode progressMode = QuotaProgressMode.InventoryOrProduction;

    [Header("Timer")]
    public bool useTimer = true;
    [Min(10f)] public float baseTimeLimitSeconds = 300f;
    [Min(0f)] public float timeReductionPerLevelSeconds = 10f;
    [Range(0.05f, 0.95f)] public float urgentTimeRatio = 0.25f;

    [Header("Scaling")]
    [Min(1)] public int startingLevel = 1;
    public bool resetOnFailure = true;
    public bool autoAdvanceAfterCompletion = true;
    [Min(0.5f)] public float nextQuotaDelaySeconds = 3f;

    [Header("State")]
    [SerializeField] private int level;
    [SerializeField] private int producedProgress;
    [SerializeField] private float timeRemaining;
    [SerializeField] private QuotaState state;

    private bool hasStarted;
    private int lastCurrent = -1;
    private int lastRequired = -1;
    private readonly Dictionary<ItemSO, int> productionByItem = new Dictionary<ItemSO, int>();

    public event Action<int, int> OnQuotaStarted;
    public event Action<int, int> OnQuotaProgressChanged;
    public event Action<int> OnQuotaCompleted;
    public event Action<int> OnQuotaFailed;
    public event Action<QuotaState> OnStateChanged;

    public int Level => level;
    public int RequiredAmount => Mathf.Max(1, baseQuantity + (level - startingLevel) * quantityIncreasePerLevel);
    public int CurrentAmount
    {
        get
        {
            int inventoryAmount = Inventory.Instance != null && targetItem != null
                ? Inventory.Instance.CountItem(targetItem)
                : 0;
            int productionAmount = targetItem != null && productionByItem.TryGetValue(targetItem, out int amount) ? amount : producedProgress;
            return progressMode switch
            {
                QuotaProgressMode.Inventory => inventoryAmount,
                QuotaProgressMode.ProductionCounter => productionAmount,
                _ => Mathf.Max(inventoryAmount, productionAmount)
            };
        }
    }
    public float TimeRemaining => timeRemaining;
    public float TimeLimit => Mathf.Max(10f, baseTimeLimitSeconds - (level - startingLevel) * timeReductionPerLevelSeconds);
    public float Progress01 => Mathf.Clamp01((float)CurrentAmount / RequiredAmount);
    public QuotaState State => state;
    public string TargetName => targetItem != null ? targetItem.itemName : targetItemName;

    private const string OfflineGameplaySceneName = "scene2";
    private const string OnlineGameplaySceneName = "gameonline";

    private static bool IsQuotaScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == OfflineGameplaySceneName || sceneName == OnlineGameplaySceneName;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!IsQuotaScene()) return;
        if (FindFirstObjectByType<QuotaSystem>() != null) return;
        GameObject quotaObject = new GameObject("Quota System");
        quotaObject.AddComponent<QuotaSystem>();
    }

    private void Awake()
    {
        if (!IsQuotaScene())
        {
            Destroy(gameObject);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        level = Mathf.Max(1, startingLevel);
        state = QuotaState.Normal;
    }

    private void Start()
    {
        ResolveTargetItem();
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += HandleInventoryChanged;
        StartQuota();
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!hasStarted || state == QuotaState.Complete || state == QuotaState.Failed) return;

        if (useTimer)
        {
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            if (timeRemaining <= 0f)
            {
                FailQuota();
                return;
            }
        }

        RefreshProgress();
    }

    private void ResolveTargetItem()
    {
        if (targetItem != null) return;
        if (Inventory.Instance != null)
        {
            foreach (ItemSO item in Inventory.Instance.knownItems)
            {
                if (item != null && string.Equals(Normalize(item.itemName), Normalize(targetItemName)))
                {
                    targetItem = item;
                    return;
                }
            }
        }

        ItemSO[] loadedItems = Resources.FindObjectsOfTypeAll<ItemSO>();
        foreach (ItemSO item in loadedItems)
        {
            if (item != null && string.Equals(Normalize(item.itemName), Normalize(targetItemName)))
            {
                targetItem = item;
                return;
            }
        }
    }

    private static string Normalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return name.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    public void StartQuota()
    {
        ResolveTargetItem();
        level = Mathf.Max(1, level);
        producedProgress = 0;
        productionByItem.Clear();
        lastCurrent = -1;
        lastRequired = -1;
        timeRemaining = TimeLimit;
        hasStarted = true;
        SetState(QuotaState.Normal);
        OnQuotaStarted?.Invoke(level, RequiredAmount);
        RefreshProgress();
    }

    public void RegisterProduction(ItemSO item, int amount)
    {
        if (!hasStarted || item == null || amount <= 0 || item != targetItem) return;
        if (productionByItem.ContainsKey(item)) productionByItem[item] += amount;
        else productionByItem[item] = amount;
        producedProgress = productionByItem[item];
        RefreshProgress();
    }

    public void CompleteQuota()
    {
        if (state == QuotaState.Complete || state == QuotaState.Failed) return;
        SetState(QuotaState.Complete);
        OnQuotaCompleted?.Invoke(level);
        if (autoAdvanceAfterCompletion)
            Invoke(nameof(AdvanceLevel), nextQuotaDelaySeconds);
    }

    public void FailQuota()
    {
        if (state == QuotaState.Complete || state == QuotaState.Failed) return;
        hasStarted = false;
        SetState(QuotaState.Failed);
        OnQuotaFailed?.Invoke(level);
        if (resetOnFailure)
        {
            level = startingLevel;
            Invoke(nameof(StartQuota), 2.5f);
        }
    }

    public void AdvanceLevel()
    {
        level++;
        StartQuota();
    }

    private void HandleInventoryChanged() => RefreshProgress();

    private void RefreshProgress()
    {
        if (!hasStarted || targetItem == null) return;
        int current = CurrentAmount;
        int required = RequiredAmount;

        if (current != lastCurrent || required != lastRequired)
        {
            lastCurrent = current;
            lastRequired = required;
            OnQuotaProgressChanged?.Invoke(current, required);
        }

        if (current >= required)
        {
            CompleteQuota();
            return;
        }

        QuotaState nextState = useTimer && timeRemaining <= TimeLimit * urgentTimeRatio
            ? QuotaState.Urgent
            : QuotaState.Normal;
        if (nextState != state) SetState(nextState);
    }

    private void SetState(QuotaState nextState)
    {
        if (state == nextState) return;
        state = nextState;
        OnStateChanged?.Invoke(state);
    }
}
