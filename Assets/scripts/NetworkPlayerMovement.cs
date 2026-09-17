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

    public float horizontalSpeed { get; private set; }
    public Vector2 CurrentInput { get; private set; }
    public bool CurrentSprint { get; private set; }
    public uint LastMovementSequence { get; private set; }
    public uint LastAcknowledgedSequence { get; private set; }
    public float LastInputSendAgeMs { get; private set; } = -1f;
    public float LastMovementAckAgeMs { get; private set; } = -1f;
    public float LastMovementCommandRttMs { get; private set; } = -1f;
    public float LastServerPositionError { get; private set; } = -1f;
    public Vector3 LastServerPosition { get; private set; }
    public int ServerReceivedSequence { get; private set; }

    private const float InputSendInterval = 1f / 30f;
    private CameraFollower cameraFollower;
    private Vector2 lastSentInput;
    private bool lastSentSprint;
    private float lastSentYRotation;
    private float inputSendTimer;
    private float lastInputSendTime = -1f;
    private float lastMovementAckTime = -1f;
    private float lastMovementSequenceSendTime = -1f;
    private uint nextMovementSequence;
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
            SimulateOwnedMovement();

        UpdateDiagnosticsAges();
    }

    private void SimulateOwnedMovement()
    {
        if (controller == null)
            return;

        CurrentInput = ReadMovementInput();
        CurrentSprint = IsSprintPressed();
        float lookYaw = cameraFollower != null ? cameraFollower.WorldYaw : transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, lookYaw, 0f);

        float speed = CurrentSprint ? sprintSpeed : walkSpeed;
        Vector3 move = transform.right * CurrentInput.x + transform.forward * CurrentInput.y;
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
            animator.SetFloat("Speed", CurrentInput.magnitude);
            animator.SetBool("Sprint", CurrentSprint);
        }

        SendMovementTelemetry(lookYaw);
    }

    private void SendMovementTelemetry(float lookYaw)
    {
        inputSendTimer += Time.unscaledDeltaTime;
        bool inputChanged = !Mathf.Approximately(CurrentInput.x, lastSentInput.x)
            || !Mathf.Approximately(CurrentInput.y, lastSentInput.y)
            || CurrentSprint != lastSentSprint
            || Mathf.Abs(Mathf.DeltaAngle(lastSentYRotation, lookYaw)) > 0.1f;

        if (!inputChanged && inputSendTimer < InputSendInterval)
            return;

        inputSendTimer = 0f;
        lastSentInput = CurrentInput;
        lastSentSprint = CurrentSprint;
        lastSentYRotation = lookYaw;
        lastInputSendTime = Time.unscaledTime;
        lastMovementSequenceSendTime = lastInputSendTime;
        LastMovementSequence = ++nextMovementSequence;
        CmdReportMovement(LastMovementSequence, transform.position, lookYaw, CurrentInput, CurrentSprint);
    }

    [Command(channel = Channels.Unreliable)]
    private void CmdReportMovement(uint sequence, Vector3 clientPosition, float clientYaw, Vector2 input, bool sprint, NetworkConnectionToClient sender = null)
    {
        ServerReceivedSequence = (int)sequence;
        LastServerPosition = clientPosition;

        if (connectionToClient != null)
            TargetMovementAck(connectionToClient, sequence, transform.position);
    }

    [TargetRpc(channel = Channels.Unreliable)]
    private void TargetMovementAck(NetworkConnectionToClient target, uint sequence, Vector3 serverPosition)
    {
        if (sequence < LastAcknowledgedSequence)
            return;

        LastAcknowledgedSequence = sequence;
        lastMovementAckTime = Time.unscaledTime;
        LastMovementAckAgeMs = 0f;
        LastMovementCommandRttMs = lastMovementSequenceSendTime >= 0f
            ? (Time.unscaledTime - lastMovementSequenceSendTime) * 1000f
            : -1f;
        LastServerPosition = serverPosition;
        LastServerPositionError = Vector3.Distance(transform.position, serverPosition);
    }

    private void UpdateDiagnosticsAges()
    {
        if (lastInputSendTime >= 0f)
            LastInputSendAgeMs = (Time.unscaledTime - lastInputSendTime) * 1000f;
        if (lastMovementAckTime >= 0f)
            LastMovementAckAgeMs = (Time.unscaledTime - lastMovementAckTime) * 1000f;
    }

    /// <summary>
    /// Applies a server-requested slip force to the local authoritative player.
    /// </summary>
    public void AddSlip(Vector3 force)
    {
        if (!isOwned)
            return;
        slipVelocity += new Vector3(force.x, 0f, force.z);
    }

    /// <summary>
    /// Clears local movement velocity after a respawn or correction.
    /// </summary>
    public void ResetVelocity()
    {
        velocity = Vector3.zero;
        slipVelocity = Vector3.zero;
        CurrentInput = Vector2.zero;
        CurrentSprint = false;
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
