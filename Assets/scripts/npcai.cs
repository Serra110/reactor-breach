using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NPC : MonoBehaviour
{
    private const string SpeedParameter = "Speed";
    private const string SprintParameter = "Sprint";
    private const string CombatParameter = "InCombat";
    private const string AttackParameter = "Attack";

    public enum State { Idle, Patrol, Alert, Chase, Attack, Retreat }

    [HideInInspector] public NavMeshAgent Agent;
    [HideInInspector] public Animator Animator;

    [Header("Target")]
    public Transform Target;

    [Header("Patrol")]
    public Transform[] manualWaypoints;
    public float waypointWaitTime = 2f;
    public float patrolSpeed = 3.5f;
    public bool autoGenerateWaypoints = true;
    public int waypointCount = 20;
    public float waypointMinSpacing = 8f;

    [Header("Detection")]
    public float sightRange = 15f;
    [Range(0f, 360f)]
    public float sightAngle = 110f;
    public float hearingRange = 20f;
    public LayerMask obstacleMask;
    public float playerSearchInterval = 0.5f;

    [Header("Chase")]
    public float chaseSpeed = 9f;
    public float chaseGiveUpTime = 6f;
    public float chaseLoseDistance = 25f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 20;

    [Header("Retreat")]
    public float retreatDistance = 12f;
    public float retreatDuration = 4f;

    [Header("Behavior")]
    public float alertDuration = 3f;
    public float idleWaitTime = 3f;

    [Header("Player Tag")]
    public string playerTag = "Player";

    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioSource voiceSource;
    public AudioClip[] footstepWalkClips;
    public AudioClip[] footstepRunClips;
    public AudioClip[] growlClips;
    public AudioClip detectSound;
    public AudioClip attackSound;
    public AudioClip deathSound;
    public bool autoSetupAudio = true;
    public float footstepMinInterval = 0.35f;
    public float growlMinInterval = 6f;
    public float maxVoiceHearingRange = 30f;

    public State CurrentState { get; private set; } = State.Idle;
    public float CurrentSpeed => Agent != null ? Agent.velocity.magnitude : 0f;

    private float stateTimer;
    private int waypointIndex;
    private float lastAttackTime;
    private Vector3 lastKnownPlayerPos;
    private bool playerDetected;
    private float playerSearchTimer;
    private Transform[] generatedWaypoints;
    private float lastFootstepTime;
    private float lastGrowlTime;
    private bool detectPlayed;

    // ===================================================================
    //  LIFECYCLE
    // ===================================================================

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        Agent.updatePosition = true;
        Agent.updateRotation = true;
        Animator.applyRootMotion = false;
        if (Agent.speed <= 0f) Agent.speed = patrolSpeed;

        SetupAudio();
    }

    private void SetupAudio()
    {
        if (!autoSetupAudio) return;

        if (footstepSource == null)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.spatialBlend = 1f;
            s.playOnAwake = false;
            s.minDistance = 2f;
            s.maxDistance = 25f;
            s.volume = 0.5f;
            s.rolloffMode = AudioRolloffMode.Linear;
            footstepSource = s;
        }

        if (voiceSource == null)
        {
            var v = gameObject.AddComponent<AudioSource>();
            v.spatialBlend = 1f;
            v.playOnAwake = false;
            v.minDistance = 5f;
            v.maxDistance = maxVoiceHearingRange;
            v.volume = 0.9f;
            v.rolloffMode = AudioRolloffMode.Linear;
            voiceSource = v;
        }
    }

    private void Start()
    {
        manualWaypoints = manualWaypoints ?? new Transform[0];
        FindNearestPlayer();
        if (Agent == null || !Agent.isOnNavMesh)
        {
            enabled = false;
            return;
        }

        if (generatedWaypoints == null)
            GenerateWaypoints();

        ChangeState(State.Idle);
    }

    private void Update()
    {
        playerSearchTimer -= Time.deltaTime;
        if (playerSearchTimer <= 0f)
        {
            FindNearestPlayer();
            playerSearchTimer = playerSearchInterval;
        }

        if (Agent.isOnNavMesh)
        {
            DetectPlayer();
            TickState();
        }

        if (Animator != null)
        {
            SetAnimatorFloat(SpeedParameter, CurrentSpeed);
            SetAnimatorBool(SprintParameter, CurrentState == State.Chase || CurrentState == State.Attack);
            SetAnimatorBool(CombatParameter, CurrentState == State.Chase || CurrentState == State.Attack);
        }

        TickAudio();
    }

    // ===================================================================
    //  AUDIO
    // ===================================================================

    private void TickAudio()
    {
        float speed = CurrentSpeed;

        if (speed > 0.1f && footstepSource != null)
        {
            float interval = footstepMinInterval;
            if (CurrentState == State.Chase || CurrentState == State.Attack)
                interval *= 0.5f;

            if (Time.time - lastFootstepTime >= interval && !footstepSource.isPlaying)
            {
                lastFootstepTime = Time.time;
                AudioClip[] pool = (CurrentState == State.Chase || CurrentState == State.Attack)
                    ? footstepRunClips
                    : footstepWalkClips;

                if (pool != null && pool.Length > 0)
                {
                    footstepSource.clip = pool[Random.Range(0, pool.Length)];
                    footstepSource.Play();
                }
            }
        }

        if (voiceSource == null) return;

        if (CurrentState == State.Chase || CurrentState == State.Attack)
        {
            if (Time.time - lastGrowlTime >= growlMinInterval && growlClips != null && growlClips.Length > 0)
            {
                lastGrowlTime = Time.time;
                voiceSource.clip = growlClips[Random.Range(0, growlClips.Length)];
                voiceSource.Play();
            }
        }
        else if (playerDetected && !detectPlayed && detectSound != null)
        {
            detectPlayed = true;
            voiceSource.PlayOneShot(detectSound);
        }
        else if (!playerDetected)
        {
            detectPlayed = false;
        }
    }

    // ===================================================================
    //  PLAYER SEARCH (multiplayer: players spawn and despawn dynamically)
    // ===================================================================

    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        if (players.Length == 0) { Target = null; return; }

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var p in players)
        {
            if (!p.activeInHierarchy) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = p.transform;
            }
        }

        Target = closest;
    }

    // ===================================================================
    //  SMART WAYPOINT GENERATION (NavMesh coverage)
    // ===================================================================

    private void GenerateWaypoints()
    {
        if (manualWaypoints != null && manualWaypoints.Length > 0)
        {
            generatedWaypoints = manualWaypoints;
            return;
        }

        if (!autoGenerateWaypoints) return;

        NavMeshHit hit;
        NavMesh.SamplePosition(transform.position, out hit, 50f, NavMesh.AllAreas);
        Bounds navBounds = CalculateNavMeshBounds();

        List<Vector3> positions = new List<Vector3>();
        int maxAttempts = waypointCount * 20;
        int attempts = 0;

        while (positions.Count < waypointCount && attempts < maxAttempts)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(navBounds.min.x, navBounds.max.x),
                navBounds.center.y,
                Random.Range(navBounds.min.z, navBounds.max.z)
            );

            if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
            {
                Vector3 candidate = hit.position;
                bool tooClose = false;

                foreach (var existing in positions)
                {
                    if (Vector3.Distance(candidate, existing) < waypointMinSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    positions.Add(candidate);
                }
            }

            attempts++;
        }

        GameObject container = new GameObject("Generated_Waypoints");
        container.transform.position = Vector3.zero;

        generatedWaypoints = new Transform[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject wp = new GameObject($"WP_{i}");
            wp.transform.SetParent(container.transform);
            wp.transform.position = positions[i];
            generatedWaypoints[i] = wp.transform;
        }
    }

    private Bounds CalculateNavMeshBounds()
    {
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        if (colliders.Length == 0)
            return new Bounds(transform.position, Vector3.one * 100f);

        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            if (colliders[i].CompareTag("Player")) continue;
            bounds.Encapsulate(colliders[i].bounds);
        }
        bounds.Expand(2f);
        bounds.min = new Vector3(bounds.min.x, transform.position.y - 2f, bounds.min.z);
        bounds.max = new Vector3(bounds.max.x, transform.position.y + 10f, bounds.max.z);
        return bounds;
    }

    // ===================================================================
    //  DETECTION (sight + hearing)
    // ===================================================================

    private void DetectPlayer()
    {
        if (Target == null) { playerDetected = false; return; }

        Vector3 dirToPlayer = Target.position - transform.position;
        float dist = dirToPlayer.magnitude;

        bool inSightRange = dist <= sightRange;
        bool inSightAngle = Vector3.Angle(transform.forward, dirToPlayer) <= sightAngle * 0.5f;
        bool hasLineOfSight = true;

        if (inSightRange && inSightAngle && obstacleMask.value != 0)
        {
            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            RaycastHit[] hits = Physics.RaycastAll(
                eyePosition,
                dirToPlayer.normalized,
                dist,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );

            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;
                if (hitTransform == Target || hitTransform.IsChildOf(Target))
                    continue;

                hasLineOfSight = false;
                break;
            }
        }

        bool canSee = inSightRange && inSightAngle && hasLineOfSight;
        bool canHear = dist <= hearingRange && PlayerIsLoud();

        if (canSee || canHear)
        {
            lastKnownPlayerPos = Target.position;
            playerDetected = true;
        }
        else
        {
            playerDetected = false;
        }
    }

    private bool PlayerIsLoud()
    {
        if (Target == null) return false;

        var movement = Target.GetComponent<CharacterController>();
        if (movement != null)
            return movement.velocity.magnitude > 2.5f;

        var rb = Target.GetComponent<Rigidbody>();
        if (rb != null)
            return rb.linearVelocity.magnitude > 2.5f;

        return false;
    }

    // ===================================================================
    //  STATE MACHINE
    // ===================================================================

    private void TickState()
    {
        switch (CurrentState)
        {
            case State.Idle:    TickIdle(); break;
            case State.Patrol:  TickPatrol(); break;
            case State.Alert:   TickAlert(); break;
            case State.Chase:   TickChase(); break;
            case State.Attack:  TickAttack(); break;
            case State.Retreat: TickRetreat(); break;
        }
    }

    private void ChangeState(State newState)
    {
        if (Agent == null || !Agent.isOnNavMesh)
            return;

        CurrentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case State.Idle:
                Agent.isStopped = true;
                Agent.speed = patrolSpeed;
                stateTimer = idleWaitTime;
                break;

            case State.Patrol:
                Agent.isStopped = false;
                Agent.speed = patrolSpeed;
                GoToNextWaypoint();
                break;

            case State.Alert:
                Agent.isStopped = false;
                Agent.speed = patrolSpeed * 0.6f;
                if (lastKnownPlayerPos != Vector3.zero)
                    Agent.SetDestination(lastKnownPlayerPos);
                stateTimer = alertDuration;
                break;

            case State.Chase:
                Agent.isStopped = false;
                Agent.speed = chaseSpeed;
                stateTimer = chaseGiveUpTime;
                break;

            case State.Attack:
                Agent.isStopped = true;
                Agent.speed = 0f;
                stateTimer = attackCooldown;
                break;

            case State.Retreat:
                Agent.isStopped = false;
                Agent.speed = chaseSpeed;
                Vector3 retreatDir = (transform.position - lastKnownPlayerPos).normalized;
                Agent.SetDestination(transform.position + retreatDir * retreatDistance);
                stateTimer = retreatDuration;
                break;
        }
    }

    // ===================================================================
    //  ESTADOS
    // ===================================================================

    private void TickIdle()
    {
        stateTimer -= Time.deltaTime;

        if (playerDetected)
        {
            lastKnownPlayerPos = Target.position;
            float dist = Vector3.Distance(transform.position, Target.position);
            ChangeState(dist <= attackRange ? State.Attack : State.Chase);
            return;
        }

        if (stateTimer <= 0f)
            ChangeState(State.Patrol);
    }

    private void TickPatrol()
    {
        if (playerDetected)
        {
            lastKnownPlayerPos = Target.position;
            float dist = Vector3.Distance(transform.position, Target.position);
            ChangeState(dist <= attackRange ? State.Attack : State.Chase);
            return;
        }

        if (!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + 0.5f)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                GoToNextWaypoint();
        }
    }

    private void TickAlert()
    {
        stateTimer -= Time.deltaTime;

        if (playerDetected)
        {
            lastKnownPlayerPos = Target.position;
            float dist = Vector3.Distance(transform.position, Target.position);
            ChangeState(dist <= attackRange ? State.Attack : State.Chase);
            return;
        }

        if (stateTimer <= 0f)
            ChangeState(State.Patrol);
    }

    private void TickChase()
    {
        if (Target == null) { ChangeState(State.Patrol); return; }

        float distToPlayer = Vector3.Distance(transform.position, Target.position);

        if (distToPlayer <= attackRange && CanAttack())
        {
            ChangeState(State.Attack);
            return;
        }

        if (playerDetected)
        {
            lastKnownPlayerPos = Target.position;
            stateTimer = chaseGiveUpTime;
        }

        Agent.SetDestination(Target.position);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f || distToPlayer > chaseLoseDistance)
            ChangeState(State.Alert);
    }

    private void TickAttack()
    {
        stateTimer -= Time.deltaTime;

        if (Target == null) { ChangeState(State.Patrol); return; }

        float dist = Vector3.Distance(transform.position, Target.position);

        // Keep pursuing during the attack so the damage window remains active.
        Agent.isStopped = false;
        Agent.SetDestination(Target.position);
        Agent.speed = chaseSpeed;

        Vector3 lookDir = (Target.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);

        if (dist > chaseLoseDistance)
        {
            ChangeState(State.Alert);
            return;
        }

        if (dist <= attackRange && CanAttack())
        {
            PerformAttack();
            stateTimer = attackCooldown;
        }
    }

    private void TickRetreat()
    {
        stateTimer -= Time.deltaTime;

        if (playerDetected)
        {
            lastKnownPlayerPos = Target.position;
            float dist = Vector3.Distance(transform.position, Target.position);
            ChangeState(dist <= attackRange ? State.Attack : State.Chase);
            return;
        }

        if (stateTimer <= 0f)
            ChangeState(State.Patrol);
    }

    // ===================================================================
    //  ACTIONS
    // ===================================================================

    private void GoToNextWaypoint()
    {
        Transform[] wps = generatedWaypoints;
        if (wps == null || wps.Length == 0) { ChangeState(State.Idle); return; }

        Agent.SetDestination(wps[waypointIndex].position);
        stateTimer = waypointWaitTime;
        waypointIndex = (waypointIndex + 1) % wps.Length;
    }

    private bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        SetAnimatorTrigger(AttackParameter);

        if (voiceSource != null && attackSound != null)
            voiceSource.PlayOneShot(attackSound);

        if (Target == null)
            return;

        float dist = Vector3.Distance(transform.position, Target.position);
        if (dist > attackRange + 0.3f)
            return;

        NetworkPlayerHealth networkHealth = Target.GetComponent<NetworkPlayerHealth>();
        if (networkHealth != null)
        {
            networkHealth.TakeDamage(attackDamage);
            return;
        }

        PlayerHealth health = Target.GetComponent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(attackDamage);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (Animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in Animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
                return true;
        }

        return false;
    }

    private void SetAnimatorFloat(string parameterName, float value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            Animator.SetFloat(parameterName, value);
    }

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            Animator.SetBool(parameterName, value);
    }

    private void SetAnimatorTrigger(string parameterName)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
            Animator.SetTrigger(parameterName);
    }

    // ===================================================================
    //  DEBUG VISUAL
    // ===================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 leftDir = Quaternion.Euler(0, -sightAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, sightAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, leftDir * sightRange);
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, rightDir * sightRange);

        if (Application.isPlaying && generatedWaypoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var wp in generatedWaypoints)
                if (wp != null) Gizmos.DrawSphere(wp.position, 0.3f);
        }

        if (Application.isPlaying)
        {
            switch (CurrentState)
            {
                case State.Chase:  Gizmos.color = Color.red; break;
                case State.Attack: Gizmos.color = new Color(1f, 0f, 0f); break;
                case State.Alert:  Gizmos.color = Color.yellow; break;
                case State.Retreat: Gizmos.color = Color.magenta; break;
                default:           Gizmos.color = Color.green; break;
            }
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.4f);
        }
    }
}
