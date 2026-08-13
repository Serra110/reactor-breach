using UnityEngine;

public class MiniDrillMachine : EletricUnit
{
    [Header("References")]
    public Transform drillBody;    public Transform drillHead;
    public AudioSource drillAudioSource;
    public AudioClip drillSound;

    [Header("Positions")]
    public float startY = 0f;
    public float preStartY = -3.37f;
    public float finalY = -4.91f;

    [Header("Movement Speeds")]
    public float fastDescendSpeed = 1.5f;
    public float slowDescendSpeed = 0.15f;

    [Header("Spin")]
    public float spinStartSpeed = 0f;
    public float spinRampSpeed = 300f;
    public float maxSpinSpeed = 800f;
    public float inspectorSpinSpeed = 0f;

    [Header("Production")]
    public float interval = 3f;
    public int metalPerTick = 1;
    public ItemSO producedItem;
    [Tooltip("Raio de procura por depósitos de minério próximos (mundo)")]
    public float oreSearchRadius = 2f;

    [Header("Optional")]
    public bool requiresPower = false;
    public bool isPowered = true;
    public bool requiresSpinning = true;

    [Header("Integrated Battery (one-time use)")]
    [Tooltip("Energia total da bateria embutida. Uma vez vazia, o drill morre para sempre.")]
    public int batteryCapacity = 1000;
    [Tooltip("Quanto de bateria gasta por segundo enquanto trabalha.")]
    public float batteryDrainPerSecond = 3.33f;
    private float batteryCharge;
    private bool batteryDead = false;

    [Header("Placement")]
    public bool isPlaced = false;

    [Header("Output")]
    public Conveyor outputConveyor;
    public Storage targetStorage;

    private float spinSpeed;
    private int state = 0;
    private float productionTimer;
    private bool isSpinning = false;
    private bool hasReachedBottom = false;
    private bool isFinished = false; // true quando o drill já completou o ciclo e deve parar para sempre
    private OreDeposit currentDeposit;

    void Start()
    {
        if (drillBody == null)
            drillBody = transform;

        AlignToGround();

        spinSpeed = spinStartSpeed;
        productionTimer = 0f;
        batteryCharge = batteryCapacity;

        if (drillAudioSource == null)
            drillAudioSource = GetComponent<AudioSource>();
    }

    public float GetBatteryCharge() => batteryCharge;
    public bool IsBatteryDead() => batteryDead;

    public override void OnDetected()
    {
        base.OnDetected();
        if (ElectricUI1.instance != null)
        {
            ElectricUI1.instance.ShowBatteryDataPanel(
                string.IsNullOrEmpty(unitName) ? "Mini Drill" : unitName,
                Mathf.RoundToInt(batteryCharge).ToString(),
                batteryCapacity.ToString());
        }
    }

    void AlignToGround()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 5f, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 50f);
        foreach (var hit in hits)
        {
            if (hit.collider == null)
                continue;
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                float bottomOffset = transform.position.y - bounds.min.y;
                Vector3 pos = transform.position;
                pos.y = hit.point.y + bottomOffset;
                transform.position = pos;
            }
            break;
        }

        Vector3 localPos = drillBody.localPosition;
        localPos.y = startY;
        drillBody.localPosition = localPos;
    }
 
    void Update()
    {
        // Se já terminou o ciclo, fica completamente parado: sem movimento, sem rotação, sem produção.
        // IMPORTANTE: não chamamos HandleSpin() aqui, porque se "inspectorSpinSpeed" estiver
        // definido no Inspector (>0), o HandleSpin ignoraria o spinSpeed e continuaria a girar
        // para sempre. Por isso forçamos isSpinning = false diretamente, sem passar por lá.
        if (isFinished)
        {
            spinSpeed = 0f;
            isSpinning = false;
            HandleAudio();  // como isSpinning é false, isto vai parar o som
            return;
        }

        // Se a bateria integrada acabou, o drill fica morto para sempre (one-time use).
        if (batteryDead)
        {
            spinSpeed = 0f;
            isSpinning = false;
            HandleAudio();  // como isSpinning é false, isto vai parar o som
            return;
        }

        switch (state)
        {
            case 0: StartDrop(); break;
            case 1: ReachPreStart(); break;
            case 2: SlowDrillDown(); break;
            case 3: FinalState(); break;
        }

        HandleSpin();
        HandleAudio();
        HandleBattery();
        HandleProduction();
    }
    void StartDrop()
    {
        MoveToY(preStartY, fastDescendSpeed, out float velY);

        if (Mathf.Abs(velY) < 0.001f)
        {
            state = 1;
        }
    }

    void ReachPreStart()
    {
        // ativa rotação aqui
        spinSpeed = Mathf.Lerp(spinSpeed, spinRampSpeed, Time.deltaTime * 2f);

        if (spinSpeed > 200f)
            state = 2;
    }

    void SlowDrillDown()
    {
        MoveToY(finalY, slowDescendSpeed, out float velY);

        if (Mathf.Abs(velY) < 0.001f)
        {
            state = 3;
            hasReachedBottom = true;
        }
    }

    void FinalState()
    {
        // Se já atingiu o fundo, volta para cima em vez de minerar
        if (hasReachedBottom)
        {
            spinSpeed = Mathf.Lerp(spinSpeed, 0f, Time.deltaTime * 2f);  // Reduz rotação
            MoveToY(startY, fastDescendSpeed, out float velY);  // Volta para cima

            if (Mathf.Abs(velY) < 0.001f)
            {
                state = 0;
                hasReachedBottom = false;
            }
        }
        else
        {
            spinSpeed = Mathf.Lerp(spinSpeed, maxSpinSpeed, Time.deltaTime * 0.5f);
        }
    }

    void HandleSpin()
    {
        if (drillHead == null) return;

        float activeSpinSpeed = inspectorSpinSpeed > 0f ? inspectorSpinSpeed : spinSpeed;
        
        // Verifica se está a girar
        isSpinning = activeSpinSpeed > 10f;
        
        drillHead.Rotate(0f, 0f, -activeSpinSpeed * Time.deltaTime);
    }

    void HandleAudio()
    {
        if (drillAudioSource == null || drillSound == null) return;

        if (isSpinning && !drillAudioSource.isPlaying)
        {
            drillAudioSource.clip = drillSound;
            drillAudioSource.loop = true;
            drillAudioSource.Play();
        }
        else if (!isSpinning && drillAudioSource.isPlaying)
        {
            drillAudioSource.Stop();
        }
    }

    void HandleBattery()
    {
        if (batteryDead)
            return;

        // Só gasta bateria quando está realmente a trabalhar (colocado e a girar/produzir)
        if (!isPlaced || (!isSpinning && !hasReachedBottom))
            return;

        batteryCharge -= batteryDrainPerSecond * Time.deltaTime;

        if (batteryCharge <= 0f)
        {
            batteryCharge = 0f;
            batteryDead = true;
            spinSpeed = 0f;
            isSpinning = false;
        }
    }

    void HandleProduction()
    {
        if (isFinished)
            return;

        if (!isPlaced)
            return;

        if (hasReachedBottom)
            return;

        if (batteryDead)
            return;

        // Procura depósito de minério válido na posição atual.
        // Se houver um depósito por perto, usa-o; se não, minera na mesma
        // (comportamento igual ao DrillMachine original, que produzia Metal direto).
        currentDeposit = FindNearbyOreDeposit(oreSearchRadius);
        if (currentDeposit != null && !currentDeposit.HasResourcesLeft())
        {
            isFinished = true;
            return;
        }

        if (requiresSpinning && !isSpinning)
            return;

        productionTimer += Time.deltaTime;

        if (productionTimer >= interval)
        {
            productionTimer = 0f;
            Produce();
        }
    }

    void Produce()
    {
        if (currentDeposit != null)
            currentDeposit.Extract(metalPerTick);

        if (outputConveyor == null)
            outputConveyor = FindNearbyConveyor();

        if (outputConveyor != null)
        {
            outputConveyor.AddItem("Metal", metalPerTick);
            return;
        }

        if (targetStorage == null)
            targetStorage = FindNearbyStorage();

        if (targetStorage != null)
        {
            targetStorage.AddItem("Metal", metalPerTick);
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
            InventoryManager.Instance.AddResource("Metal", metalPerTick);
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

    void MoveToY(float targetY, float speed, out float velocityY)
    {
        Vector3 pos = drillBody.localPosition;
        float beforeY = pos.y;

        pos.y = Mathf.MoveTowards(pos.y, targetY, speed * Time.deltaTime);
        drillBody.localPosition = pos;

        // Velocidade real deste frame. Quando já não há mais distância a percorrer,
        // Mathf.MoveTowards não move nada e isto dá exatamente 0 - independente
        // de erros de arredondamento na posição.
        velocityY = (Time.deltaTime > 0f) ? (pos.y - beforeY) / Time.deltaTime : 0f;
    }

    OreDeposit FindNearbyOreDeposit(float radius = 2f)
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
        productionTimer = 0f;
    }

}