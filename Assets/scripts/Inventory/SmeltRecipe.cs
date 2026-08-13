using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Smelter/Recipe")]
public class SmeltRecipe : ScriptableObject
{
    public ItemSO inputItem;
    public int inputAmount = 1;

    public ItemSO outputItem;
    public int outputAmount = 1;

    [Tooltip("Tempo em segundos para completar a fundição")]
    public float smeltTime = 4f;
}
