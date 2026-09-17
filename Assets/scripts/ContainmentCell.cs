using UnityEngine;

/// <summary>
/// Cela de contencao SCP. Prende um NPC dentro da cela via porta.
/// Interacao: fechar/abrir porta da cela (emergencia).
/// Estado de breach: quando o SCP escapa (porta aberta ou danificada).
/// </summary>
public class ContainmentCell : MonoBehaviour
{
    public enum CellState { Secured, Occupied, Breached }

    [Header("Cell")]
    public SecurityDoor cellDoor;
    public Transform scpAnchor;
    public NPC containedNPC;
    public float breachCheckInterval = 1f;

    [Header("State")]
    public CellState State { get; private set; } = CellState.Secured;

    [Header("Lights")]
    public Light cellLight;
    public Color securedColor = Color.green;
    public Color occupiedColor = Color.blue;
    public Color breachColor = Color.red;

    [Header("Alarm")]
    public AudioSource alarmSource;
    public AudioClip breachAlarm;
    public AudioClip decontainmentAlarm;

    private Vector3 anchorPos;
    private float breachTimer;

    private void Start()
    {
        if (scpAnchor != null) anchorPos = scpAnchor.position;

        if (containedNPC != null)
            State = CellState.Occupied;

        UpdateLights();
    }

    private void Update()
    {
        breachTimer -= Time.deltaTime;
        if (breachTimer > 0f) return;
        breachTimer = breachCheckInterval;

        CheckContainment();
    }

    // E-key interaction
    public void Interact()
    {
        if (cellDoor != null)
            cellDoor.Interact();

        if (containedNPC != null && State == CellState.Breached)
        {
            if (cellDoor != null && cellDoor.State != SecurityDoor.DoorState.Closed)
            {
                Debug.Log("[ContainmentCell] A reter SCP... feche a porta!");
            }
        }
    }

    private void CheckContainment()
    {
        if (containedNPC == null)
        {
            if (State != CellState.Breached)
            {
                State = CellState.Breached;
                OnBreach();
            }
            UpdateLights();
            return;
        }

        float scpDist = Vector3.Distance(containedNPC.transform.position, anchorPos);
        bool insideCell = scpDist < 4f;

        if (!insideCell)
        {
            if (State != CellState.Breached)
            {
                State = CellState.Breached;
                OnBreach();
            }
        }
        else if (State == CellState.Breached && cellDoor != null
                 && cellDoor.State == SecurityDoor.DoorState.Closed)
        {
            State = CellState.Occupied;
        }
        else if (State == CellState.Secured && containedNPC != null)
        {
            State = CellState.Occupied;
        }

        UpdateLights();
    }

    private void OnBreach()
    {
        Debug.Log("[ContainmentCell] CONTAINMENT BREACH!");
        PlayAlarm(breachAlarm);
    }

    // Marca o SCP como contido (used when placing a new NPC inside)
    public void AssignNPC(NPC npc)
    {
        containedNPC = npc;
        State = CellState.Occupied;
        UpdateLights();
    }

    public void ReleaseNPC()
    {
        containedNPC = null;
        State = CellState.Breached;
    }

    private void UpdateLights()
    {
        if (cellLight == null) return;

        switch (State)
        {
            case CellState.Secured:  cellLight.color = securedColor; break;
            case CellState.Occupied: cellLight.color = occupiedColor; break;
            case CellState.Breached: cellLight.color = breachColor; break;
        }
    }

    private void PlayAlarm(AudioClip clip)
    {
        if (alarmSource != null && clip != null)
            alarmSource.PlayOneShot(clip);
    }
}
