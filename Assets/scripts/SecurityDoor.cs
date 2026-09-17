using UnityEngine;

/// <summary>
/// Porta de seguranca SCP com arco de keycard.
/// Nivel de clearance necessario >= doorLevel para abrir.
/// Funciona com o item no hotbar selecionado, ou por arco manual no inspector.
/// </summary>
public class SecurityDoor : MonoBehaviour
{
    public enum DoorState { Closed, Opening, Open, Closing }

    [Header("Door")]
    public Transform doorPanel;
    public Vector3 openOffset = new Vector3(0f, 2.4f, 0f);
    public float openSpeed = 2f;
    public bool startsOpen = false;

    [Header("Security")]
    public int requiredLevel = 1;
    public Keycard keycardItem;
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip deniedSound;

    [Header("Auto Close")]
    public bool autoClose = true;
    public float autoCloseDelay = 5f;

    public DoorState State { get; private set; } = DoorState.Closed;
    public bool IsLocked { get; private set; }

    private Vector3 closedPos;
    private Vector3 openPos;
    private float autoCloseTimer;
    private bool everOpened;
    private bool isMoving;

    public bool IsMoving => isMoving;

    private void Awake()
    {
        if (doorPanel == null) doorPanel = transform;
        closedPos = doorPanel.localPosition;
        openPos = closedPos + openOffset;

        if (startsOpen)
        {
            doorPanel.localPosition = openPos;
            State = DoorState.Open;
        }
    }

    private void Update()
    {
        float target = 0f;

        switch (State)
        {
            case DoorState.Opening:
                target = openPos.y;
                doorPanel.localPosition = Vector3.MoveTowards(
                    doorPanel.localPosition, openPos, openSpeed * Time.deltaTime);
                if (doorPanel.localPosition.y >= openPos.y - 0.01f)
                {
                    State = DoorState.Open;
                    autoCloseTimer = autoCloseDelay;
                }
                break;

            case DoorState.Closing:
                doorPanel.localPosition = Vector3.MoveTowards(
                    doorPanel.localPosition, closedPos, openSpeed * Time.deltaTime);
                if (doorPanel.localPosition.y <= closedPos.y + 0.01f)
                    State = DoorState.Closed;
                break;

            case DoorState.Open:
                if (autoClose && everOpened)
                {
                    autoCloseTimer -= Time.deltaTime;
                    if (autoCloseTimer <= 0f)
                        Close();
                }
                break;
        }

        isMoving = State == DoorState.Opening || State == DoorState.Closing;
    }

    // E-key interaction (via PlayerInteraction)
    public void Interact()
    {
        if (IsLocked) return;

        if (State == DoorState.Open || State == DoorState.Closing)
        {
            Close();
            return;
        }

        if (HasRequiredClearance())
        {
            Open();
        }
        else
        {
            PlayDenied();
        }
    }

    public void Open()
    {
        if (IsLocked) return;
        if (State == DoorState.Open || State == DoorState.Opening) return;
        State = DoorState.Opening;
        everOpened = true;
        PlaySound(openSound);
    }

    public void Close()
    {
        if (State == DoorState.Closed || State == DoorState.Closing) return;
        State = DoorState.Closing;
        PlaySound(closeSound);
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        if (locked) Close();
    }

    private bool HasRequiredClearance()
    {
        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return false;

        int selected = inv.GetSelectedHotbarIndex();
        if (selected < 0) return false;

        if (!inv.TryGetHotbarItem(selected, out ItemSO item, out _)) return false;
        if (item == null) return false;

        if (item is Keycard keycard)
            return keycard.clearanceLevel >= requiredLevel;

        return keycardItem != null && item == keycardItem;
    }

    private void PlayDenied()
    {
        PlaySound(deniedSound);
        Debug.Log("[SecurityDoor] Acesso negado! Precisa de Keycard nivel " + requiredLevel + " no hotbar.");
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2.4f, 0.4f));
    }
}
