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

    [Header("Output")]
    public Conveyor outputConveyor;
    public Storage targetStorage;

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
        // prefer explicit output conveyor
        if (outputConveyor == null)
            outputConveyor = FindNearbyConveyor();

        if (outputConveyor != null)
        {
            outputConveyor.AddItem("Metal", metalPerTick);
            Debug.Log("Drill produced " + metalPerTick + " Metal -> conveyor");
            return;
        }

        // then explicit/nearby storage
        if (targetStorage == null)
            targetStorage = FindNearbyStorage();

        if (targetStorage != null)
        {
            targetStorage.AddItem("Metal", metalPerTick);
            Debug.Log("Drill produced " + metalPerTick + " Metal -> storage");
            return;
        }

        // fallback to inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddResource("Metal", metalPerTick);
            Debug.Log("Drill produced " + metalPerTick + " Metal -> inventory");
        }
    }

    Conveyor FindNearbyConveyor(float radius = 1.5f)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
        foreach (var c in cols)
        {
            var conveyor = c.GetComponentInParent<Conveyor>();
            if (conveyor != null)
                return conveyor;
        }

        return null;
    }

    Storage FindNearbyStorage(float radius = 1.5f)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
        foreach (var c in cols)
        {
            var storage = c.GetComponentInParent<Storage>();
            if (storage != null)
                return storage;
        }

        return null;
    }


    public void SetPlaced()
    {
        isPlaced = true;
        timer = 0f;
    }
}