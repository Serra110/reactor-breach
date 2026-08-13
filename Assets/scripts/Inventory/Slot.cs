using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Slot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Material highlightMaterial;

    private ItemSO heldItem;
    private int heldAmount;
    private Image itemIcon;
    private TextMeshProUGUI amountText;
    private Image slotImage;
    private Material normalMaterial;
    private bool isSelected;

    private int slotIndex = -1;
    private bool isHotbar;
    private ReactorBreach.InventorySystem.Inventory parentInventory;

    private static GameObject _dragIconGO;
    public static int _dragFromIndex = -1;
    public static bool _dragFromHotbar;
    public static SmelterSlotUI _dragFromSmelterSlot;

    public bool hovering;

    private float _lastClickTime;
    private const float DoubleClickThreshold = 0.28f;

    private void Awake()
    {
        if (transform.childCount > 0)
            itemIcon = transform.GetChild(0).GetComponent<Image>();
        if (transform.childCount > 1)
            amountText = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        slotImage = GetComponent<Image>();
        if (slotImage != null)
            normalMaterial = slotImage.material;
    }

    public void Init(int index, ReactorBreach.InventorySystem.Inventory inventory, bool hotbar)
    {
        slotIndex = index;
        parentInventory = inventory;
        isHotbar = hotbar;
    }

    public void Display(ItemSO item, int amount)
    {
        heldItem = item;
        heldAmount = amount;

        if (item != null && amount > 0)
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = true;
                itemIcon.sprite = item.Icon;
            }
            if (amountText != null)
                amountText.text = amount > 1 ? amount.ToString() : "";
        }
        else
        {
            if (itemIcon != null) itemIcon.enabled = false;
            if (amountText != null) amountText.text = "";
        }
    }

    public bool HasItem()
    {
        return heldItem != null && heldAmount > 0;
    }

    public ItemSO GetItem()
    {
        return heldItem;
    }

    public int GetAmount()
    {
        return heldAmount;
    }

    public void SetItem(ItemSO item, int amount)
    {
        amount = Mathf.Max(0, amount);
        heldItem = item;
        heldAmount = item != null && amount > 0 ? amount : 0;
        Display(heldItem, heldAmount);
    }

    public int RemoveAmount(int amount)
    {
        if (amount <= 0 || heldItem == null || heldAmount <= 0)
            return heldAmount;

        heldAmount = Mathf.Max(0, heldAmount - amount);
        if (heldAmount <= 0)
            ClearSlot();
        else
            Display(heldItem, heldAmount);

        return heldAmount;
    }

    public void ClearSlot()
    {
        heldItem = null;
        heldAmount = 0;
        Display(null, 0);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (slotImage == null) return;

        if (selected && highlightMaterial != null)
            slotImage.material = highlightMaterial;
        else if (!selected)
            slotImage.material = normalMaterial;
    }

    public int GetSlotIndex() => slotIndex;
    public ItemSO GetHeldItem() => heldItem;
    public int GetHeldAmount() => heldAmount;

    public void Refresh()
    {
        if (parentInventory != null)
            parentInventory.RefreshAllSlotUIs();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        Debug.Log($"[Slot] PointerEnter {gameObject.name} idx={slotIndex} hotbar={isHotbar} at={eventData.position} screen={Screen.width}x{Screen.height}");
        if (slotImage != null && highlightMaterial != null && !isSelected)
            slotImage.material = highlightMaterial;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        Debug.Log($"[Slot] PointerExit {gameObject.name} idx={slotIndex} hotbar={isHotbar}");
        if (slotImage != null && !isSelected)
            slotImage.material = normalMaterial;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[Slot] PointerClick {gameObject.name} idx={slotIndex} hotbar={isHotbar} button={eventData.button}");
        if (parentInventory == null || slotIndex < 0) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            parentInventory.SplitStack(slotIndex, isHotbar);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            float timeSinceLast = Time.unscaledTime - _lastClickTime;
            _lastClickTime = Time.unscaledTime;

            if (timeSinceLast <= DoubleClickThreshold)
                parentInventory.UseItem(slotIndex, isHotbar);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (heldItem == null || parentInventory == null) return;

        Debug.Log($"[Slot] OnBeginDrag: {heldItem.itemName} from slot {slotIndex}");

        ReactorBreach.InventorySystem.InventorySlot._dragFromIndex = -1;
        _dragFromIndex = slotIndex;
        _dragFromHotbar = isHotbar;

        Transform dragLayer = parentInventory.dragLayer;
        if (dragLayer == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) dragLayer = canvas.transform;
        }

        if (dragLayer != null && itemIcon != null)
        {
            _dragIconGO = new GameObject("DragIcon");
            _dragIconGO.transform.SetParent(dragLayer, false);
            var img = _dragIconGO.AddComponent<Image>();
            img.sprite = itemIcon.sprite;
            img.raycastTarget = false;
            var rect = _dragIconGO.GetComponent<RectTransform>();
            rect.sizeDelta = ((RectTransform)itemIcon.transform).sizeDelta;
        }

        if (itemIcon != null)
            itemIcon.color = new Color(1, 1, 1, 0.35f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIconGO != null)
            _dragIconGO.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIconGO != null) Destroy(_dragIconGO);
        _dragFromIndex = -1;
        _dragFromHotbar = false;
        ReactorBreach.InventorySystem.InventorySlot._dragFromIndex = -1;
        _dragFromSmelterSlot = null;
        if (parentInventory != null) parentInventory.RefreshAllSlotUIs();
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[Slot] OnDrop on {gameObject.name} | _dragFromIndex={_dragFromIndex} | _dragFromHotbar={_dragFromHotbar} | targetIndex={slotIndex} | isHotbar={isHotbar} | smelterDrag={(Slot._dragFromSmelterSlot != null ? Slot._dragFromSmelterSlot.gameObject.name : "NULL")}");

        if (Slot._dragFromSmelterSlot != null && Slot._dragFromSmelterSlot.slotType == SmelterSlotUI.SlotType.Output)
        {
            var controller = Slot._dragFromSmelterSlot.controller;
            if (controller != null && controller.GetCurrentOutput() != null && controller.GetCurrentOutputAmount() > 0)
            {
                int added = parentInventory.AddItem(controller.GetCurrentOutput(), controller.GetCurrentOutputAmount());
                int collected = controller.GetCurrentOutputAmount() - added;
                if (collected > 0)
                    controller.CollectOutput(collected);
            }
            Slot._dragFromSmelterSlot = null;
            return;
        }

        if (_dragFromIndex < 0 || parentInventory == null)
        {
            Debug.Log($"[Slot] OnDrop blocked on {gameObject.name} | parentNull={parentInventory == null} | _dragFromIndex={_dragFromIndex} | slotIndex={slotIndex}");
            return;
        }

        if (_dragFromIndex == slotIndex && _dragFromHotbar == isHotbar)
            return;

        parentInventory.MoveOrMergeSlot(_dragFromIndex, _dragFromHotbar, slotIndex, isHotbar);
    }
}
