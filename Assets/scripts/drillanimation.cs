using UnityEngine;

public class DrillMachine : MonoBehaviour
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

    [Header("Optional")]
    public bool requiresPower = false;
    public bool isPowered = true;
    public bool requiresSpinning = true;

    [Header("Debug/Testing")]
    public int debugAddMetal = 0;
    public bool addToInventoryDebug = false;

    [Header("Placement")]
    public bool isPlaced = false;

    [Header("Output")]
    public Conveyor outputConveyor;
    public Storage targetStorage;

    private float spinSpeed;
    private int state = 0;
    private float productionTimer;
    private bool isSpinning = false;

    void Start()
    {
        if (drillBody == null)
            drillBody = transform;

        Vector3 pos = drillBody.localPosition;
        pos.y = startY;
        drillBody.localPosition = pos;

        spinSpeed = spinStartSpeed;
        productionTimer = 0f;

        Debug.Log("DrillMachine started at position: " + transform.position + " | isPlaced: " + isPlaced);

        // Se não tiver AudioSource, tenta criar um
        if (drillAudioSource == null)
            drillAudioSource = GetComponent<AudioSource>();
    }
 
    void Update()
    {
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
        HandleDebug();
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
        // Não faz nada se ainda não foi colocado no mundo
        if (!isPlaced)
        {
            Debug.Log("Production blocked: not placed");
            return;
        }

        // Energia (se quiseres usar depois)
        if (requiresPower && !isPowered)
        {
            Debug.Log("Production blocked: no power");
            return;
        }

        // Só produz se estiver a girar (se isso for obrigatório)
        if (requiresSpinning && !isSpinning)
        {
            Debug.Log("Production blocked: requires spinning but not spinning");
            return;
        }

        productionTimer += Time.deltaTime;

        if (productionTimer >= interval)
        {
            productionTimer = 0f;
            Produce();
        }
    }

    void Produce()
    {
        Debug.Log("Produce() called - checking output targets");
        
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
        else
        {
            Debug.LogWarning("Drill production failed: no conveyor, storage, or inventory!");
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

    void HandleDebug()
    {
        if (debugAddMetal > 0 && addToInventoryDebug)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddResource("Metal", debugAddMetal);
                Debug.Log("Debug: Added " + debugAddMetal + " Metal to inventory");
            }
            else
            {
                Debug.LogWarning("InventoryManager not found!");
            }

            // Reset
            debugAddMetal = 0;
            addToInventoryDebug = false;
        }
    }
}