using UnityEngine;


public class OreDeposit : MonoBehaviour
{
    [Tooltip("Tipo de recurso deste depósito. Tem de bater certo com o que a Drill/Inventário esperam (ex: \"Metal\", \"Uranium\")")]
    public string resourceType;

    [Tooltip("Quantidade total disponível. -1 = infinito (útil para testar já sem te preocupares com esgotar)")]
    public int amountRemaining = -1;

    [Tooltip("Quantidade máxima que o depósito pode conter (regenera até este valor)")]
    public int maxAmount = 100;

    [Tooltip("Raio à volta do centro do depósito onde uma Drill é considerada 'em cima' dele")]
    public float detectionRadius = 2f;
    [Tooltip("Offset (local) do centro de deteção se o modelo não estiver alinhado com o pivot")]
    public Vector3 detectionOffset = Vector3.zero;

    [Header("Regeneration")]
    [Tooltip("Se true, o depósito regenera recursos ao longo do tempo")]
    public bool regenerate = true;
    [Tooltip("Quantos recursos regenera por ciclo")]
    public int regenAmount = 1;
    [Tooltip("Intervalo em segundos entre cada regeneração")]
    public float regenInterval = 10f;

    private float regenTimer;

    private void Update()
    {
        if (!regenerate || amountRemaining < 0) return;
        if (amountRemaining >= maxAmount) return;

        regenTimer += Time.deltaTime;
        if (regenTimer >= regenInterval)
        {
            regenTimer = 0f;
            amountRemaining = Mathf.Min(amountRemaining + regenAmount, maxAmount);
        }
    }

    public bool HasResourcesLeft()
    {
        return amountRemaining < 0 || amountRemaining > 0;
    }


    public void Extract(int amount = 1)
    {
        if (amountRemaining < 0) return;
        amountRemaining = Mathf.Max(0, amountRemaining - amount);
    }

    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.4f);
        // Desenha a esfera de deteção usando o offset local (transform.TransformPoint para converter em world)
        Vector3 worldCenter = transform.TransformPoint(detectionOffset);
        Gizmos.DrawSphere(worldCenter, detectionRadius);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 1f);
        Gizmos.DrawWireSphere(worldCenter, detectionRadius);
    }

    // Utilitário para outros scripts (Drill, Inventory) verificarem se uma posição está dentro do depósito
    public bool IsWithinRange(Vector3 worldPosition)
    {
        Vector3 worldCenter = transform.TransformPoint(detectionOffset);
        return Vector3.SqrMagnitude(worldPosition - worldCenter) <= detectionRadius * detectionRadius;
    }
}