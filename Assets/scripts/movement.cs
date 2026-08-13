using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Animator animator;

    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;

    [HideInInspector] public float horizontalSpeed = 0f;

    [Header("Footsteps")]
    public AudioSource footstepSource;

    public AudioClip walkClip;
    public AudioClip sprintClip;

    public float stepDistanceWalk = 2.2f;
    public float stepDistanceSprint = 3.2f;

    private Vector3 lastFootstepPosition;
    private Vector3 _velocity;
    private Vector3 _slipVelocity;

    void Start()
    {
        lastFootstepPosition = transform.position;
    }

    public void ResetVelocity()
    {
        _velocity = Vector3.zero;
    }

    // Faz o jogador deslizar numa direção (ex: casca de banana). A velocidade é
    // amortecida gradualmente até parar.
    public void AddSlip(Vector3 force)
    {
        _slipVelocity += new Vector3(force.x, 0f, force.z);
    }

    void Update()
    {
        float x = GetAxis("Horizontal");
        float z = GetAxis("Vertical");

        bool sprint = IsSprintPressed();
        float speed = sprint ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;

        if (controller != null)
            controller.Move(move * speed * Time.deltaTime);

        horizontalSpeed = move.magnitude * speed;

        // FOOTSTEPS SYSTEM (baseado em distância, à prova de isGrounded a piscar)
        bool hasInput = move.magnitude > 0.1f;

        if (hasInput)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastFootstepPosition);
            float stepDistance = sprint ? stepDistanceSprint : stepDistanceWalk;

            if (distanceMoved >= stepDistance && footstepSource != null)
            {
                AudioClip clipToPlay = sprint ? sprintClip : walkClip;
                footstepSource.PlayOneShot(clipToPlay);
                lastFootstepPosition = transform.position;
            }
        }
        else
        {
            // só reseta quando o jogador REALMENTE parou de dar input
            lastFootstepPosition = transform.position;
        }

        // Gravity
        if (controller != null)
        {
            if (controller.isGrounded && _velocity.y < 0)
                _velocity.y = -2f;

            _velocity.y += gravity * Time.deltaTime;
            controller.Move(_velocity * Time.deltaTime);
        }

        // Slip (cascas de banana e afins) — desliza com amortecimento
        if (controller != null && _slipVelocity.sqrMagnitude > 0.0001f)
        {
            _slipVelocity = Vector3.Lerp(_slipVelocity, Vector3.zero, Time.deltaTime * 3f);
            controller.Move(_slipVelocity * Time.deltaTime);
        }

        // Animations
        float moveAmount = new Vector2(x, z).magnitude;
        if (animator != null)
        {
            animator.SetFloat("Speed", moveAmount);
            animator.SetBool("Sprint", sprint);
        }
    }

    private float GetAxis(string axisName)
    {
        float value = Input.GetAxisRaw(axisName);
        if (Mathf.Abs(value) > 0.01f)
            return value;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            switch (axisName)
            {
                case "Horizontal":
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                        return -1f;
                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                        return 1f;
                    break;
                case "Vertical":
                    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                        return -1f;
                    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                        return 1f;
                    break;
            }
        }
#endif

        return 0f;
    }

    private bool IsSprintPressed()
    {
        bool sprint = Input.GetKey(KeyCode.LeftShift);

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            sprint = sprint || Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
#endif

        return sprint;
    }
}