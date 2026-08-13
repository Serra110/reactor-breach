using UnityEngine;

public class MiningDrill : ElectricDevice
{
    [Header("Production")]
    public float interval = 3f;
    public int metalPerTick = 1;
    public ItemSO producedItem;
    [Tooltip("Raio de procura por depósitos de minério próximos (mundo)")]
    public float oreSearchRadius = 3f;
    [Tooltip("Se true, o drill pára permanentemente quando o depósito local esgota")]
    public bool stopWhenEmpty = true;

    private bool isFinished = false;

    private float timer;

    [Header("Optional")]
    public bool requiresPower = false;
    public bool isPowered = true;

    [Header("Placement")]
    public bool isPlaced = false;

    [Header("Output")]
    public Conveyor outputConveyor;
    public Storage targetStorage;

    public override void Start()
    {
        base.Start();
    }

    bool HasGridPower()
    {
        if (inputPorts == null || inputPorts.Count == 0)
            return !requiresPower;

        return isDeviceActive || totalInput > 0;
    }

    void Update()
    {
        if (isFinished) return;
        //  Não faz nada se ainda não foi colocado no mundo
        if (!isPlaced)
            return;

        // Energia: se estiver ligado à rede, só trabalha com energia
        if (!HasGridPower())
            return;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            // Verifica existência de depósito próximo antes de produzir
            var deposit = FindNearbyOreDeposit(oreSearchRadius);
            if (deposit == null)
            {
                timer = 0f;
                return;
            }

            if (!deposit.HasResourcesLeft())
            {
                if (stopWhenEmpty)
                {
                    isFinished = true;
                    isPlaced = false;
                }
                timer = 0f;
                return;
            }

            timer = 0f;
            Produce(deposit);
        }
    }

    void Produce(OreDeposit deposit)
    {
        string itemName = producedItem != null ? producedItem.itemName : "Metal";

        if (deposit != null)
            deposit.Extract(metalPerTick);

        if (outputConveyor == null)
            outputConveyor = FindNearbyConveyor();

        if (outputConveyor != null)
        {
            outputConveyor.AddItem(itemName, metalPerTick);
            return;
        }

        if (targetStorage == null)
            targetStorage = FindNearbyStorage();

        if (targetStorage != null)
        {
            targetStorage.AddItem(itemName, metalPerTick);
            return;
        }

        if (producedItem != null)
        {
            var inv = ReactorBreach.InventorySystem.Inventory.Instance;
            if (inv != null)
            {
                inv.AddItem(producedItem, metalPerTick);
                return;
            }
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddResource(itemName, metalPerTick);
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

    OreDeposit FindNearbyOreDeposit(float radius = 3f)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
        foreach (var c in cols)
        {
            var deposit = c.GetComponentInParent<OreDeposit>();
            if (deposit != null && deposit.IsWithinRange(transform.position))
                return deposit;
        }

        return null;
    }


    public void SetPlaced()
    {
        isPlaced = true;
        timer = 0f;
    }
}