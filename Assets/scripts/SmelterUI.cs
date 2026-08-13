using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SmelterUI : MonoBehaviour
{
    [Header("Controller")]
    public SmelterController controller;

    [Header("Panel")]
    public GameObject panel;

    [Header("Slots")]
    public SmelterSlotUI inputSlot;
    public SmelterSlotUI outputSlot;

    [Header("Progress")]
    public Image progressBarFill;

    [Header("Status")]
    public TextMeshProUGUI statusText;

    private bool isOpen;

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        ConfigureProgressBar();
    }

    private void ConfigureProgressBar()
    {
        if (progressBarFill == null) return;
        progressBarFill.type = Image.Type.Filled;
        progressBarFill.fillMethod = Image.FillMethod.Horizontal;
        progressBarFill.fillOrigin = 0;
        progressBarFill.fillClockwise = true;
        progressBarFill.fillAmount = 0f;
    }

    private void OnEnable()
    {
        if (controller == null) return;
        controller.OnInputChanged += HandleInputChanged;
        controller.OnProgressChanged += HandleProgressChanged;
        controller.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (controller == null) return;
        controller.OnInputChanged -= HandleInputChanged;
        controller.OnOutputChanged -= HandleOutputChanged;
        controller.OnProgressChanged -= HandleProgressChanged;
        controller.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Tab))
            Close();
    }

    public void Open()
    {
        if (panel == null) return;

        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv != null && inv.Container != null && inv.Container.activeInHierarchy)
            inv.Container.SetActive(false);

        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        if (panel.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            panel.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        panel.SetActive(true);
        isOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshUI();
    }

    public void Close()
    {
        if (panel == null) return;
        panel.SetActive(false);
        isOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public bool IsOpen() => isOpen;

    private void RefreshUI()
    {
        if (controller == null) return;
        ConfigureProgressBar();
        HandleInputChanged(controller.GetCurrentInput(), controller.GetCurrentInputAmount());
        HandleOutputChanged(controller.GetCurrentOutput(), controller.GetCurrentOutputAmount());
        HandleProgressChanged(controller.GetProgress());
        HandleStateChanged(controller.GetState());
    }

    private void HandleInputChanged(ItemSO item, int amount)
    {
        if (inputSlot != null)
            inputSlot.UpdateDisplay(item, amount);
    }

    private void HandleOutputChanged(ItemSO item, int amount)
    {
        if (outputSlot != null)
            outputSlot.UpdateDisplay(item, amount);
    }

    private void HandleProgressChanged(float p)
    {
        if (progressBarFill != null)
            progressBarFill.fillAmount = p;
    }

    private void HandleStateChanged(SmelterController.State state)
    {
        if (statusText == null) return;

        switch (state)
        {
            case SmelterController.State.Idle:
                statusText.text = "Idle";
                break;
            case SmelterController.State.Processing:
                statusText.text = "Processing...";
                break;
            case SmelterController.State.OutputFull:
                statusText.text = "Output full";
                break;
        }
    }
}
