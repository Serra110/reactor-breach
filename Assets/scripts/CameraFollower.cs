using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraFollower : MonoBehaviour
{
    [Header("Referências")]
    public Transform playerBody;
    public PlayerMovement playerMovement;
    public NetworkPlayerMovement networkPlayerMovement;

    [Header("Ocultar Corpo em Primeira Pessoa")]
    public bool esconderPartesDoCorpo = true;
    public GameObject[] partesParaEsconder;

    [Header("Position (captured with the button below)")]
    [Tooltip("Eye height relative to the center of the player.")]
    public float eyeHeight = 1.7f;
    [Tooltip("Offset horizontal (X) e profundidade (Z) em relação ao player.")]
    public Vector3 positionOffset = Vector3.zero;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float verticalClamp = 80f;

    [Header("HeadBob")]
    public float bobFrequency = 1.8f;
    public float bobAmplitudeY = 0.008f;
    public float bobAmplitudeX = 0.004f;
    public float bobReturnSpeed = 12f;

    [Header("Ferramenta de Alinhamento")]
    [ContextMenuItem("Capture Current Position", "CapturarOffsets")]
    [TextArea(1, 2)]
    public string instrucao = "Right-click here to capture the camera position!";

    private float _pitch;
    private float _yaw;
    private bool _lookInitialized;
    private float _bobTimer;
    private Vector3 _bobCurrent;
    private Renderer[][] _cachedPartRenderers;
    private bool[] _partHasRenderers;

    public float WorldYaw => _yaw;

    [ContextMenu("Capture Current Position")]
    public void CapturarOffsets()
    {
        if (playerBody == null)
        {
            Debug.LogError("Por favor, atribui o 'Player Body' antes de capturar!");
            return;
        }

        // Calculate the camera position relative to the player in the Editor.
        Vector3 posicaoRelativa = playerBody.InverseTransformPoint(transform.position);

        // Separa os valores nos teus inputs automaticamente
        eyeHeight = posicaoRelativa.y;
        positionOffset = new Vector3(posicaoRelativa.x, 0f, posicaoRelativa.z);

        Debug.Log($"[Success] Position captured! Height: {eyeHeight} | X/Z offset: {positionOffset}");
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
        if (networkPlayerMovement == null)
            networkPlayerMovement = GetComponentInParent<NetworkPlayerMovement>();
        if (!_lookInitialized && playerBody != null)
        {
            _yaw = playerBody.eulerAngles.y;
            _lookInitialized = true;
        }

        CachePartRenderers();
        ConfigureFirstPersonVisibility();
    }

    void CachePartRenderers()
    {
        if ((partesParaEsconder == null || partesParaEsconder.Length == 0) && playerBody != null)
            partesParaEsconder = new[] { playerBody.gameObject };
        if (partesParaEsconder == null) return;

        _partHasRenderers = new bool[partesParaEsconder.Length];
        _cachedPartRenderers = new Renderer[partesParaEsconder.Length][];

        for (int i = 0; i < partesParaEsconder.Length; i++)
        {
            if (partesParaEsconder[i] != null)
            {
                _cachedPartRenderers[i] = partesParaEsconder[i].GetComponentsInChildren<Renderer>(true);
                _partHasRenderers[i] = _cachedPartRenderers[i].Length > 0;
            }
        }
    }

    private void ConfigureFirstPersonVisibility()
    {
        Camera playerCamera = GetComponent<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera != null) playerCamera.cullingMask &= ~(1 << 2);
        if (partesParaEsconder == null || _cachedPartRenderers == null) return;

        for (int i = 0; i < _cachedPartRenderers.Length; i++)
        {
            Renderer[] renderers = _cachedPartRenderers[i];
            if (renderers == null) continue;
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] != null)
                    renderers[r].gameObject.layer = 2;
            }
        }
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null) return;
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void LateUpdate()
    {
        if (playerBody == null) return;

        float movementSpeed = networkPlayerMovement != null ? networkPlayerMovement.horizontalSpeed : (playerMovement != null ? playerMovement.horizontalSpeed : 0f);
        bool estaAMover = movementSpeed > 0.1f;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = GetMouseAxis("Mouse X") * mouseSensitivity;
        float mouseY = GetMouseAxis("Mouse Y") * mouseSensitivity;

        _yaw += mouseX;
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -verticalClamp, verticalClamp);

        Vector3 bobTarget = Vector3.zero;
        if (estaAMover)
        {
            float speed = movementSpeed;
            _bobTimer += Time.deltaTime * bobFrequency * Mathf.Clamp(speed / 5f, 0.3f, 1.5f);
            bobTarget = new Vector3(
                Mathf.Sin(_bobTimer) * bobAmplitudeX,
                Mathf.Abs(Mathf.Sin(_bobTimer)) * bobAmplitudeY,
                0f);
        }
        else
        {
            _bobTimer = 0f;
        }

        _bobCurrent = Vector3.Lerp(_bobCurrent, bobTarget, bobReturnSpeed * Time.deltaTime);

        // Keep the camera relative to CameraPivot. The player root owns network yaw,
        // so only the local yaw offset and pitch belong on the camera.
        float localYaw = Mathf.DeltaAngle(playerBody.eulerAngles.y, _yaw);
        transform.localRotation = Quaternion.Euler(_pitch, localYaw, 0f);

        Transform parent = transform.parent;
        if (parent != null)
        {
            Vector3 parentLocalPos = parent.InverseTransformPoint(playerBody.position + Vector3.up * eyeHeight);
            transform.localPosition = new Vector3(
                positionOffset.x + _bobCurrent.x,
                parentLocalPos.y + positionOffset.y + _bobCurrent.y,
                positionOffset.z);
        }
        else
        {
            Vector3 basePos = playerBody.position + Vector3.up * eyeHeight;
            basePos += playerBody.right * positionOffset.x;
            basePos += playerBody.up * positionOffset.y;
            basePos += playerBody.forward * positionOffset.z;
            transform.position = basePos + transform.right * _bobCurrent.x + transform.up * _bobCurrent.y;
        }
    }

    private float GetMouseAxis(string axisName)
    {
        float value = Input.GetAxisRaw(axisName);
        if (Mathf.Abs(value) > 0.01f)
            return value;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (axisName == "Mouse X")
                return Mouse.current.delta.x.ReadValue();
            if (axisName == "Mouse Y")
                return Mouse.current.delta.y.ReadValue();
        }
#endif

        return 0f;
    }

    private void GerenciarVisibilidadeCorpo(bool deveEsconder)
    {
        if (!esconderPartesDoCorpo || partesParaEsconder == null || _cachedPartRenderers == null) return;

        bool estadoRenderer = !deveEsconder;

        for (int i = 0; i < partesParaEsconder.Length; i++)
        {
            if (partesParaEsconder[i] == null) continue;

            if (_partHasRenderers[i])
            {
                Renderer[] renderers = _cachedPartRenderers[i];
                for (int r = 0; r < renderers.Length; r++)
                {
                    if (renderers[r] != null && renderers[r].enabled != estadoRenderer)
                        renderers[r].enabled = estadoRenderer;
                }
            }
            else
            {
                if (partesParaEsconder[i].activeSelf != estadoRenderer)
                    partesParaEsconder[i].SetActive(estadoRenderer);
            }
        }
    }
}