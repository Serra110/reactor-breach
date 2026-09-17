using UnityEngine;

/// <summary>ItemSO para keycards de seguranca com nivel de clearance.</summary>
[CreateAssetMenu(fileName = "Keycard", menuName = "SCP/Keycard")]
public class Keycard : ItemSO
{
    [Tooltip("Nivel de clearance: L1 = acesso basico, L5 = acesso total.")]
    public int clearanceLevel = 1;

    [Tooltip("Cor do cartao.")]
    public Color cardColor = Color.white;
}
