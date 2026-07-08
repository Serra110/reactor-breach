using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public TMP_Text text;

    void Update()
    {
        if (text == null)
            return;

        var manager = InventoryManager.Instance;
        text.text =
            "Metal: " + manager.GetResourceAmount("Metal") + "\n" +
            "Uranium: " + manager.GetResourceAmount("Uranium") + "\n" +
            "Money: " + manager.GetResourceAmount("Money");
    }
}