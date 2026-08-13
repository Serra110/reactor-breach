using UnityEngine;
[CreateAssetMenu(fileName = "Item", menuName = "NewItem")]
public class ItemSO : ScriptableObject
{
public string itemName;
public Sprite Icon;
public int maxStackSize = 99;
public GameObject itemPrefab;
public GameObject handItemPrefab;
}
