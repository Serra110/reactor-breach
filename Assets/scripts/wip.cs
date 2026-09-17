using Mirror;
using UnityEngine;
using UnityEngine.UI;

public sealed class wip : MonoBehaviour
{
    [SerializeField] private GameObject wipPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject triggerPlane;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool showDebugLogs;

    private CanvasGroup wipCanvasGroup;
    private bool noticeShown;
    private bool noticeCompleted;

    private void Awake()
    {
        ResolveReferences();
        PrepareWipPanel();
        SetWipVisible(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (triggerPlane == null)
            Debug.LogError("[WIP] Atribui o plano em Trigger Plane no Inspector.", this);
        if (wipPanel == null)
            Debug.LogError("[WIP] Atribui o painel WIP em Wip Panel no Inspector.", this);

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(HideWipNotice);
            continueButton.onClick.AddListener(HideWipNotice);
        }
    }

    private void Start()
    {
        SetWipVisible(false);
    }

    private void Update()
    {
        if (noticeCompleted || noticeShown)
            return;

        if (IsLocalPlayerOnPlane())
        {
            OpenWipNotice();
            return;
        }

        // This fallback also works when the network player spawns after this object.
        Collider planeCollider = GetTriggerCollider();
        if (planeCollider == null)
            return;

        Collider[] contacts = Physics.OverlapBox(
            planeCollider.bounds.center,
            planeCollider.bounds.extents,
            Quaternion.identity);

        foreach (Collider contact in contacts)
        {
            if (IsPlayer(contact.gameObject))
            {
                OpenWipNotice();
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryOpenForPlayer(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryOpenForPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryOpenForPlayer(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryOpenForPlayer(collision.gameObject);
    }

    public void OpenWipNotice()
    {
        if (noticeCompleted || noticeShown || wipPanel == null)
            return;

        noticeShown = true;
        SetWipVisible(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideWipNotice()
    {
        if (wipPanel == null)
            return;

        SetWipVisible(false);
        noticeShown = false;
        noticeCompleted = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void TryOpenForPlayer(GameObject other)
    {
        if (IsPlayer(other))
            OpenWipNotice();
    }

    private bool IsPlayer(GameObject other)
    {
        Transform playerRoot = other.transform.root;
        if (other.CompareTag(playerTag) || playerRoot.CompareTag(playerTag))
            return true;

        if (NetworkClient.localPlayer != null
            && playerRoot == NetworkClient.localPlayer.transform.root)
            return true;

        NetworkIdentity identity = other.GetComponentInParent<NetworkIdentity>();
        return identity != null
            && (identity.isLocalPlayer || identity == NetworkClient.localPlayer);
    }

    private bool IsLocalPlayerOnPlane()
    {
        Transform playerTransform = FindLocalPlayerTransform();
        if (playerTransform == null)
            return false;

        GameObject plane = triggerPlane != null ? triggerPlane : gameObject;
        Renderer planeRenderer = plane.GetComponentInChildren<Renderer>(true);
        Collider planeCollider = plane.GetComponentInChildren<Collider>(true);
        Bounds bounds;

        if (planeCollider != null)
            bounds = planeCollider.bounds;
        else if (planeRenderer != null)
            bounds = planeRenderer.bounds;
        else
            return false;

        bounds.Expand(new Vector3(0f, 4f, 0f));
        bool inside = bounds.Contains(playerTransform.position);

        if (showDebugLogs && inside)
            Debug.Log("[WIP] Jogador local entrou no plano.", this);

        return inside;
    }

    private static Transform FindLocalPlayerTransform()
    {
        if (NetworkClient.localPlayer != null)
            return NetworkClient.localPlayer.transform;

        NetworkPlayerMovement[] networkPlayers = FindObjectsByType<NetworkPlayerMovement>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        foreach (NetworkPlayerMovement player in networkPlayers)
        {
            if (player.isOwned)
                return player.transform;
        }

        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (taggedPlayers.Length > 0)
            return taggedPlayers[0].transform;

        return null;
    }

    private Collider GetTriggerCollider()
    {
        GameObject plane = triggerPlane != null ? triggerPlane : gameObject;
        Collider collider = plane.GetComponentInChildren<Collider>(true);
        if (collider != null)
            return collider;

        Renderer planeRenderer = plane.GetComponentInChildren<Renderer>(true);
        if (planeRenderer == null)
            return null;

        BoxCollider generatedCollider = planeRenderer.gameObject.AddComponent<BoxCollider>();
        Bounds bounds = planeRenderer.bounds;
        Vector3 localMin = planeRenderer.transform.InverseTransformPoint(bounds.min);
        Vector3 localMax = planeRenderer.transform.InverseTransformPoint(bounds.max);
        generatedCollider.center = (localMin + localMax) * 0.5f;
        generatedCollider.size = new Vector3(
            Mathf.Abs(localMax.x - localMin.x),
            Mathf.Max(Mathf.Abs(localMax.y - localMin.y), 0.2f),
            Mathf.Abs(localMax.z - localMin.z));
        return generatedCollider;
    }

    private void PrepareWipPanel()
    {
        if (wipPanel == null)
            return;

        wipCanvasGroup = wipPanel.GetComponent<CanvasGroup>();
        if (wipCanvasGroup == null)
            wipCanvasGroup = wipPanel.AddComponent<CanvasGroup>();
    }

    private void SetWipVisible(bool visible)
    {
        if (wipCanvasGroup == null)
            return;

        wipCanvasGroup.alpha = visible ? 1f : 0f;
        wipCanvasGroup.interactable = visible;
        wipCanvasGroup.blocksRaycasts = visible;
    }

    private void ResolveReferences()
    {
        if (wipPanel == null)
            wipPanel = FindChildByName("WIPPanel", "WIP", "WorkInProgress");

        if (wipPanel == null)
            wipPanel = FindSceneObjectByName("WIPPanel", "WIP", "WorkInProgress");

        if (continueButton == null && wipPanel != null)
            continueButton = FindButtonInChildren(wipPanel, "Continue", "Continuar");

        if (continueButton == null)
            continueButton = FindSceneButtonByName("Continue", "Continuar");

        if (wipPanel == null && continueButton != null && continueButton.transform.parent != null)
            wipPanel = continueButton.transform.parent.gameObject;
    }

    private GameObject FindChildByName(params string[] names)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            foreach (string name in names)
            {
                if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }
        }

        return null;
    }

    private static Button FindButtonInChildren(GameObject root, params string[] names)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            foreach (string name in names)
            {
                if (string.Equals(button.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return button;
            }
        }

        return null;
    }

    private static GameObject FindSceneObjectByName(params string[] names)
    {
        Transform[] objects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform candidate in objects)
        {
            foreach (string name in names)
            {
                if (string.Equals(candidate.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return candidate.gameObject;
            }
        }

        return null;
    }

    private static Button FindSceneButtonByName(params string[] names)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            foreach (string name in names)
            {
                if (string.Equals(button.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return button;
            }
        }

        return null;
    }
}
