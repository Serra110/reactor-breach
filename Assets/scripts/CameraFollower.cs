using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraFollower : MonoBehaviour
{
    [Header("Referências")]
    public Transform playerBody;
    public PlayerMovement playerMovement;

    [Header("Ocultar Corpo em Primeira Pessoa")]
    public bool esconderPartesDoCorpo = true;
    public GameObject[] partesParaEsconder;

    [Header("Posição (Preenchida pelo Botão abaixo)")]
    [Tooltip("Altura dos olhos em relação ao centro do player.")]
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
    [ContextMenuItem("Capturar Posição Atual", "CapturarOffsets")]
    [TextArea(1, 2)]
    public string instrucao = "Clique com o botão direito aqui para capturar a posição da câmara!";

    private float _pitch;
    private float _bobTimer;
    private Vector3 _bobCurrent;

    // Esta função calcula o offset com base em onde meteste a câmara na cena
    [ContextMenu("Capturar Posição Atual")]
    public void CapturarOffsets()
    {
        if (playerBody == null)
        {
            Debug.LogError("Por favor, atribui o 'Player Body' antes de capturar!");
            return;
        }

        // Calcula onde a câmara está em relação ao Player no Editor
        Vector3 posicaoRelativa = playerBody.InverseTransformPoint(transform.position);

        // Separa os valores nos teus inputs automaticamente
        eyeHeight = posicaoRelativa.y;
        positionOffset = new Vector3(posicaoRelativa.x, 0f, posicaoRelativa.z);

        Debug.Log($"[Sucesso] Posição capturada! Altura: {eyeHeight} | Offset X/Z: {positionOffset}");
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void LateUpdate()
    {
        if (playerBody == null) return;

        bool estaAMover = (playerMovement != null && playerMovement.horizontalSpeed > 0.1f);

        GerenciarVisibilidadeCorpo(estaAMover);

        // Aplica os valores que foram capturados pelo botão
        Vector3 basePos = playerBody.position + Vector3.up * eyeHeight;
        basePos += playerBody.right * positionOffset.x;
        basePos += playerBody.up * positionOffset.y; 
        basePos += playerBody.forward * positionOffset.z;

        // Rotação do Mouse
        float mouseX = GetMouseAxis("Mouse X") * mouseSensitivity;
        float mouseY = GetMouseAxis("Mouse Y") * mouseSensitivity;

        playerBody.Rotate(Vector3.up * mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -verticalClamp, verticalClamp);

        transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // Lógica do HeadBob
        Vector3 bobTarget = Vector3.zero;

        if (estaAMover)
        {
            float speed = playerMovement.horizontalSpeed;
            _bobTimer += Time.deltaTime * bobFrequency * Mathf.Clamp(speed / 5f, 0.3f, 1.5f);

            bobTarget = new Vector3(
                Mathf.Sin(_bobTimer) * bobAmplitudeX,
                Mathf.Abs(Mathf.Sin(_bobTimer)) * bobAmplitudeY,
                0f
            );
        }
        else
        {
            _bobTimer = 0f;
        }

        _bobCurrent = Vector3.Lerp(_bobCurrent, bobTarget, bobReturnSpeed * Time.deltaTime);

        transform.position = basePos
            + transform.right * _bobCurrent.x
            + transform.up * _bobCurrent.y;
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
        if (!esconderPartesDoCorpo || partesParaEsconder == null) return;

        bool estadoRenderer = !deveEsconder;

        foreach (GameObject parte in partesParaEsconder)
        {
            if (parte != null)
            {
                Renderer[] renderers = parte.GetComponentsInChildren<Renderer>(true);
                
                if (renderers.Length > 0)
                {
                    foreach (Renderer r in renderers)
                    {
                        if (r.enabled != estadoRenderer) r.enabled = estadoRenderer;
                    }
                }
                else
                {
                    if (parte.activeSelf != estadoRenderer) parte.SetActive(estadoRenderer);
                }
            }
        }
    }
}