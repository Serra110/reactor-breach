using UnityEngine;

public class DrillMachine : ElectricDevice
{
    [Header("References")]
    public Transform drillBody;
    public Transform drillHead;
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

    [Header("Optional")]
    public bool requiresPower = false;
    public bool isPowered = true;
    public bool requiresSpinning = true;

    [Header("Placement")]
    public bool isPlaced = false;

    [Header("Output")]
    public Conveyor outputConveyor;
    public Storage targetStorage;

    private float spinSpeed;
    private int state = 0;
    private float productionTimer;
    private bool isSpinning = false;

    public override void Start()
    {
        base.Start();

        if (drillBody == null)
            drillBody = transform;

        AlignToGround();

        spinSpeed = spinStartSpeed;
        productionTimer = 0f;

        if (drillAudioSource == null)
            drillAudioSource = GetComponent<AudioSource>();
    }

    bool HasGridPower()
    {
        if (inputPorts == null || inputPorts.Count == 0)
            return !requiresPower;

        return isDeviceActive || totalInput > 0;
    }        
    

    void AlignToGround()
    {
        // Raycast down from current position to find the ground
        Ray ray = new Ray(transform.position + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            // Move the whole drill so its base sits on the ground
            // We need to account for the bounds of the drill model
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
        }

        // Reset local Y to startY
        Vector3 localPos = drillBody.localPosition;
        localPos.y = startY;
        drillBody.localPosition = localPos;
    }
 
    void Update()
    {
        // SEM ENERGIA: a broca não desce, não roda e não produz.
        // A rotação desacelera até parar (e o som também pára).
        if (!HasGridPower())
        {
            spinSpeed = Mathf.Lerp(spinSpeed, 0f, Time.deltaTime * 2f);
            isSpinning = spinSpeed > 10f;
            HandleAudio();
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
        HandleProduction();
    }
    void StartDrop()
    {
        MoveToY(preStartY, fastDescendSpeed);

        if (AtY(preStartY))
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
        MoveToY(finalY, slowDescendSpeed);

        if (AtY(finalY))
        {
            state = 3;
        }
    }

    void FinalState()
    {
        spinSpeed = Mathf.Lerp(spinSpeed, maxSpinSpeed, Time.deltaTime * 0.5f);
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

    void HandleProduction()
    {
        if (!isPlaced)
            return;

        if (!HasGridPower())
            return;

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
        string itemName = producedItem != null ? producedItem.itemName : "Metal";

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

    void MoveToY(float targetY, float speed)
    {
        Vector3 pos = drillBody.localPosition;

        pos.y = Mathf.MoveTowards(pos.y, targetY, speed * Time.deltaTime);

        drillBody.localPosition = pos;
    }

    bool AtY(float y)
    {
        return Mathf.Abs(drillBody.localPosition.y - y) < 0.01f;
    }

    public void SetPlaced()
    {
        isPlaced = true;
        productionTimer = 0f;
    }
}