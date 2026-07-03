using UnityEngine;

public class MiningDrill : MonoBehaviour
{
    [Header("Production")]
    public float interval = 3f;
    public int metalPerTick = 1;

    private float timer;

    [Header("Optional")]
    public bool requiresPower = false;
    public bool isPowered = true;

    [Header("Placement")]
    public bool isPlaced = false;

    void Update()
    {
        //  Não faz nada se ainda não foi colocado no mundo
        if (!isPlaced)
            return;

        // Energia (se quiseres usar depois)
        if (requiresPower && !isPowered)
            return;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;
            Produce();
        }
    }

    void Produce()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.AddResource("Metal", metalPerTick);
        Debug.Log(" Drill produziu " + metalPerTick + " Metal");
    }

   
    public void SetPlaced()
    {
        isPlaced = true;
        timer = 0f;
    }
}