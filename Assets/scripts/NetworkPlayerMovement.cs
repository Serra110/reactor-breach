using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class NetworkPlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public CharacterController controller;
    public Animator animator;
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;

    [SyncVar]
    public float horizontalSpeed;

    private const float InputSendInterval = 1f / 30f;

    private CameraFollower cameraFollower;
    private Vector2 pendingInput;
    private bool pendingSprint;
    private float pendingYRotation;
    private Vector2 lastSentInput;
    private bool lastSentSprint;
    private float lastSentYRotation;
    private float inputSendTimer;
    private bool hasSentInput;
    private Vector3 velocity;
    private Vector3 slipVelocity;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        cameraFollower = GetComponentInChildren<CameraFollower>(true);
    }

    private void Update()
    {
        if (isOwned)
            SendCurrentInput();

        if (isServer)
            SimulateMovement();
    }

    private void SendCurrentInput()
    {
        Vector2 input = ReadMovementInput();
        bool sprint = IsSprintPressed();
        float lookYaw = cameraFollower != null ? cameraFollower.WorldYaw : transform.eulerAngles.y;
        inputSendTimer += Time.unscaledDeltaTime;

        bool inputChanged = !hasSentInput || input != lastSentInput || sprint != lastSentSprint || Mathf.Abs(Mathf.DeltaAngle(lastSentYRotation, lookYaw)) > 0.1f;
        if (!inputChanged && inputSendTimer < InputSendInterval)
            return;

        inputSendTimer = 0f;
        lastSentInput = input;
        lastSentSprint = sprint;
        lastSentYRotation = lookYaw;
        hasSentInput = true;
        CmdSetInput(input, sprint, lookYaw);
    }

    [Command(channel = Channels.Unreliable)]
    private void CmdSetInput(Vector2 input, bool sprint, float yRotation)
    {
        pendingInput = Vector2.ClampMagnitude(input, 1f);
        pendingSprint = sprint;
        pendingYRotation = yRotation;
    }

    [Server]
    private void SimulateMovement()
    {
        if (controller == null)
            return;

        transform.rotation = Quaternion.Euler(0f, pendingYRotation, 0f);
        float speed = pendingSprint ? sprintSpeed : walkSpeed;
        Vector3 move = transform.right * pendingInput.x + transform.forward * pendingInput.y;
        controller.Move(move * speed * Time.deltaTime);
        horizontalSpeed = move.magnitude * speed;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (slipVelocity.sqrMagnitude > 0.0001f)
        {
            slipVelocity = Vector3.Lerp(slipVelocity, Vector3.zero, Time.deltaTime * 3f);
            controller.Move(slipVelocity * Time.deltaTime);
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetFloat("Speed", pendingInput.magnitude);
            animator.SetBool("Sprint", pendingSprint);
        }
    }

    [Server]
    public void AddSlip(Vector3 force)
    {
        slipVelocity += new Vector3(force.x, 0f, force.z);
    }

    [Server]
    public void ResetVelocity()
    {
        velocity = Vector3.zero;
        slipVelocity = Vector3.zero;
        pendingInput = Vector2.zero;
        pendingSprint = false;
    }

    private Vector2 ReadMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Mathf.Abs(horizontal) < 0.01f)
                horizontal = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
            if (Mathf.Abs(vertical) < 0.01f)
                vertical = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f);
        }
#endif
        return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
    }

    private bool IsSprintPressed()
    {
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            sprint |= Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
#endif
        return sprint;
    }
}
