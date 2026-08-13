using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SmelterSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
{
    public enum SlotType { Input, Output }

    [Header("Config")]
    public SlotType slotType;
    public SmelterController controller;

    [Header("Visual")]
    public Image iconImage;
    public TextMeshProUGUI amountText;

    private ItemSO heldItem;
    private int heldAmount;

    private static GameObject _dragIconGO;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();

        if (iconImage != null)
            iconImage.raycastTarget = false;

        Image myImage = GetComponent<Image>();
        if (myImage == null)
        {
            myImage = gameObject.AddComponent<Image>();
            myImage.color = new Color(0, 0, 0, 0.01f);
        }
        myImage.raycastTarget = true;

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;

        if (amountText == null)
            amountText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[SmelterSlotUI] PointerDown on {gameObject.name} | Type: {slotType}");
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[SmelterSlotUI] PointerClick on {gameObject.name} | Type: {slotType}");
    }

    public void UpdateDisplay(ItemSO item, int amount)
    {
        heldItem = item;
        heldAmount = amount;

        if (item != null && amount > 0)
        {
            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = item.Icon;
            }
            if (amountText != null)
                amountText.text = amount > 1 ? amount.ToString() : "";
        }
        else
        {
            if (iconImage != null) iconImage.enabled = false;
            if (amountText != null) amountText.text = "";
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (slotType != SlotType.Input || controller == null) return;

        var draggedSlot = eventData.pointerDrag?.GetComponent<Slot>();
        if (draggedSlot == null) return;

        ItemSO item = draggedSlot.GetHeldItem();
        if (item == null) return;

        int amount = draggedSlot.GetHeldAmount();

        if (controller.TryInsertInputFromInventory(item, amount))
        {
            draggedSlot.Refresh();
            Debug.Log($"[SmelterSlotUI] Dropped {amount}x {item.itemName} into smelter");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotType != SlotType.Output || heldItem == null || heldAmount <= 0) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform dragLayer = canvas != null ? canvas.transform : null;

        if (dragLayer != null && iconImage != null)
        {
            _dragIconGO = new GameObject("DragIcon");
            _dragIconGO.transform.SetParent(dragLayer, false);
            var img = _dragIconGO.AddComponent<Image>();
            img.sprite = iconImage.sprite;
            img.raycastTarget = false;
            var rect = _dragIconGO.GetComponent<RectTransform>();
            rect.sizeDelta = ((RectTransform)iconImage.transform).sizeDelta;
        }

        Slot._dragFromSmelterSlot = this;

        if (iconImage != null)
            iconImage.color = new Color(1, 1, 1, 0.35f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIconGO != null)
            _dragIconGO.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIconGO != null) Destroy(_dragIconGO);
        Slot._dragFromSmelterSlot = null;

        if (iconImage != null)
            iconImage.color = Color.white;
    }
}
