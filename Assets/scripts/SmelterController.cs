using System;
using UnityEngine;

public class SmelterController : MonoBehaviour
{
    public enum State { Idle, Processing, OutputFull }

    [Header("UI")]
    public SmelterUI linkedUI;

    [Header("Recipes")]
    public SmeltRecipe[] possibleRecipes;

    [Header("Config")]
    public int outputCapacity = 99;

    [Header("Output to Inventory")]
    [Tooltip("Se true, output vai diretamente para o player inventory quando possivel")]
    public bool autoCollectToInventory = true;

    [Header("State (debug)")]
    [SerializeField] private ItemSO currentInput;
    [SerializeField] private int currentInputAmount;
    [SerializeField] private ItemSO currentOutput;
    [SerializeField] private int currentOutputAmount;
    [SerializeField] private float progress;
    [SerializeField] private State state = State.Idle;

    private SmeltRecipe activeRecipe;
    private float timer;

    public event Action<ItemSO, int> OnInputChanged;
    public event Action<ItemSO, int> OnOutputChanged;
    public event Action<float> OnProgressChanged;
    public event Action<State> OnStateChanged;

    private void Update()
    {
        if (state != State.Processing) return;

        timer += Time.deltaTime;
        progress = Mathf.Clamp01(timer / activeRecipe.smeltTime);
        OnProgressChanged?.Invoke(progress);

        if (timer >= activeRecipe.smeltTime)
            CompleteSmelt();
    }

    public bool TryInsertInput(ItemSO item, int amount)
    {
        if (state == State.Processing) return false;
        if (currentInput != null && currentInput != item) return false;
        if (item == null) return false;

        currentInput = item;
        currentInputAmount += amount;
        OnInputChanged?.Invoke(currentInput, currentInputAmount);

        TryStartSmelt();
        return true;
    }

    public bool TryInsertInputFromInventory(ItemSO item, int amount)
    {
        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return false;
        if (!inv.HasItem(item, amount)) return false;

        if (TryInsertInput(item, amount))
        {
            inv.RemoveItem(item, amount);
            return true;
        }
        return false;
    }

    private void TryStartSmelt()
    {
        if (state == State.Processing) return;

        SmeltRecipe recipe = FindRecipeFor(currentInput);
        if (recipe == null) return;
        if (currentInputAmount < recipe.inputAmount) return;

        if (currentOutput != null && currentOutput != recipe.outputItem) return;
        if (currentOutputAmount + recipe.outputAmount > outputCapacity) return;

        activeRecipe = recipe;
        timer = 0f;
        progress = 0f;
        SetState(State.Processing);
        OnProgressChanged?.Invoke(0f);
    }

    private void CompleteSmelt()
    {
        currentInputAmount -= activeRecipe.inputAmount;
        if (currentInputAmount <= 0)
        {
            currentInputAmount = 0;
            currentInput = null;
        }
        OnInputChanged?.Invoke(currentInput, currentInputAmount);

        currentOutput = activeRecipe.outputItem;
        currentOutputAmount += activeRecipe.outputAmount;
        OnOutputChanged?.Invoke(currentOutput, currentOutputAmount);

        if (QuotaSystem.Instance != null)
            QuotaSystem.Instance.RegisterProduction(activeRecipe.outputItem, activeRecipe.outputAmount);


        progress = 0f;
        OnProgressChanged?.Invoke(0f);

        SetState(State.Idle);

        if (autoCollectToInventory)
            TryAutoCollect();

        TryStartSmelt();
    }

    public int CollectOutput(int amount)
    {
        int collected = Mathf.Min(amount, currentOutputAmount);
        currentOutputAmount -= collected;

        if (currentOutputAmount <= 0)
        {
            currentOutputAmount = 0;
            currentOutput = null;
        }

        OnOutputChanged?.Invoke(currentOutput, currentOutputAmount);

        if (state == State.Idle) TryStartSmelt();
        return collected;
    }

    public int CollectOutputToInventory(int amount)
    {
        if (currentOutput == null) return 0;

        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return 0;

        ItemSO outputItem = currentOutput;
        int collected = CollectOutput(amount);
        if (collected > 0)
            inv.AddItem(outputItem, collected);

        return collected;
    }

    private void TryAutoCollect()
    {
        if (currentOutput == null || currentOutputAmount <= 0) return;

        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return;

        ItemSO outputItem = currentOutput;
        int remaining = inv.AddItem(outputItem, currentOutputAmount);
        int collected = currentOutputAmount - remaining;
        if (collected > 0)
            CollectOutput(collected);
    }

    public ItemSO GetCurrentInput() => currentInput;
    public int GetCurrentInputAmount() => currentInputAmount;
    public ItemSO GetCurrentOutput() => currentOutput;
    public int GetCurrentOutputAmount() => currentOutputAmount;
    public float GetProgress() => progress;
    public State GetState() => state;

    public void Interact()
    {
        if (linkedUI != null && linkedUI.IsOpen())
        {
            if (currentOutput != null && currentOutputAmount > 0)
                CollectOutputToInventory(currentOutputAmount);

            linkedUI.Close();
            return;
        }

        InsertFromHotbar();

        if (linkedUI != null)
            linkedUI.Open();
    }

    private void InsertFromHotbar()
    {
        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return;

        int selectedIdx = inv.GetSelectedHotbarIndex();
        if (selectedIdx < 0) return;

        if (!inv.TryGetHotbarItem(selectedIdx, out ItemSO item, out int amount) || item == null)
        {
            Debug.Log("[SmelterController] InsertFromHotbar: hotbar slot is empty");
            return;
        }

        Debug.Log($"[SmelterController] InsertFromHotbar trying 1x {item.itemName} from hotbar slot {selectedIdx}");

        if (TryInsertInputFromInventory(item, 1))
            Debug.Log($"[SmelterController] InsertFromHotbar inserted 1x {item.itemName}");
        else
            Debug.Log($"[SmelterController] InsertFromHotbar FAILED (probably wrong item)");
    }

    private SmeltRecipe FindRecipeFor(ItemSO item)
    {
        if (item == null) return null;
        foreach (var recipe in possibleRecipes)
        {
            if (recipe != null && recipe.inputItem == item)
                return recipe;
        }
        return null;
    }

    private void SetState(State newState)
    {
        if (state == newState) return;
        state = newState;
        OnStateChanged?.Invoke(state);
    }
}
