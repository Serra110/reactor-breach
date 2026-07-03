using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public TMP_Text text;
    void Update()
    {
        text.text =
            "Metal: " + InventoryManager.Instance.GetResourceAmount("Metal") + "\n" +
            "Uranium: " + InventoryManager.Instance.GetResourceAmount("Uranium") + "\n" +
            "Money: " + InventoryManager.Instance.GetResourceAmount("Money");
    }
}