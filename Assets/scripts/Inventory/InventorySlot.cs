using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ReactorBreach.InventorySystem
{
    public class InventorySlot : MonoBehaviour,
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
        private Inventory parentInventory;

        private static GameObject _dragIconGO;
        public static int _dragFromIndex = -1;

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

        public void Init(int index, Inventory inventory)
        {
            slotIndex = index;
            parentInventory = inventory;
        }

        public void Display(ItemSO item, int amount)
        {
            heldItem = item;
            heldAmount = amount;

            if (item != null && amount > 0)
            {
                itemIcon.enabled = true;
                itemIcon.sprite = item.Icon;
                amountText.text = amount > 1 ? amount.ToString() : "";
            }
            else
            {
                itemIcon.enabled = false;
                amountText.text = "";
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            Debug.Log($"[InventorySlot] PointerEnter on {gameObject.name} | slotIndex={slotIndex}");
            if (slotImage != null && highlightMaterial != null && !isSelected)
                slotImage.material = highlightMaterial;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            Debug.Log($"[InventorySlot] PointerExit on {gameObject.name} | slotIndex={slotIndex}");
            if (slotImage != null && !isSelected)
                slotImage.material = normalMaterial;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (parentInventory == null || slotIndex < 0) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                parentInventory.SplitStack(slotIndex);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                float timeSinceLast = Time.unscaledTime - _lastClickTime;
                _lastClickTime = Time.unscaledTime;

                if (timeSinceLast <= DoubleClickThreshold)
                    parentInventory.UseItem(slotIndex);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (heldItem == null || parentInventory == null) return;

            Debug.Log($"[InventorySlot] OnBeginDrag: {heldItem.itemName} x{heldAmount} from slot {slotIndex}");

            Slot._dragFromIndex = -1;
            _dragFromIndex = slotIndex;

            Transform dragLayer = parentInventory.dragLayer;
            if (dragLayer == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null) dragLayer = canvas.transform;
            }

            if (dragLayer != null)
            {
                _dragIconGO = new GameObject("DragIcon");
                _dragIconGO.transform.SetParent(dragLayer, false);
                var img = _dragIconGO.AddComponent<Image>();
                img.sprite = itemIcon.sprite;
                img.raycastTarget = false;
                var rect = _dragIconGO.GetComponent<RectTransform>();
                rect.sizeDelta = ((RectTransform)itemIcon.transform).sizeDelta;
            }

            itemIcon.color = new Color(1, 1, 1, 0.35f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragIconGO != null)
                _dragIconGO.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"[InventorySlot] OnEndDrag on {gameObject.name} | slotIndex={slotIndex}");
            if (_dragIconGO != null) Destroy(_dragIconGO);
            _dragFromIndex = -1;
            Slot._dragFromIndex = -1;
            if (parentInventory != null) parentInventory.RefreshAllSlotUIs();
        }

        public void OnDrop(PointerEventData eventData)
        {
            int sourceIndex = _dragFromIndex;
            string sourceType = "InventorySlot";

            if (sourceIndex < 0 && Slot._dragFromIndex >= 0)
            {
                sourceIndex = Slot._dragFromIndex;
                sourceType = "Slot";
            }

            Debug.Log($"[InventorySlot] OnDrop on {gameObject.name} | sourceType={sourceType} | sourceIndex={sourceIndex} | targetIndex={slotIndex} | pointerDrag={(eventData.pointerDrag != null ? eventData.pointerDrag.name : "NULL")}");

            if (sourceIndex < 0 || parentInventory == null)
            {
                Debug.Log($"[InventorySlot] OnDrop blocked on {gameObject.name} | parentNull={parentInventory == null} | sourceIndex={sourceIndex} | targetIndex={slotIndex}");
                return;
            }

            parentInventory.MoveOrMergeSlot(sourceIndex, false, slotIndex, false);
        }
    }
}
