using UnityEngine;
using UnityEngine.AI;

 [RequireComponent(typeof(NavMeshAgent))]
 [AddComponentMenu("Game/Navigation/NPC Wander")]
 public class NPCWander : MonoBehaviour
    {
        public Transform WanderCenter;

        [Min(0f)]
        public float WanderRadius = 20f;

        [Min(1)]
        public int SampleAttempts = 10;

        [Min(0f)]
        public float NavMeshSampleDistance = 2f;

        public Transform Target;

        [Min(0f)]
        public float WaitAtDestination = 2f;

        [Min(0.01f)]
        public float DestinationCheckInterval = 0.2f;

        private NavMeshAgent agent;
        private float waitTimer;
        private float destinationCheckTimer;
        private bool warnedAboutNavMesh;
        private bool warnedAboutDestination;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;

            if (agent.speed <= 0f)
            {
                agent.speed = 3.5f;
            }

            ChooseNextDestination();
        }

        private void Update()
        {
            if (Target != null)
            {
                return;
            }

            if (!agent.isOnNavMesh)
            {
                if (!warnedAboutNavMesh)
                {
                    Debug.LogWarning(name + " is not on a NavMesh. Bake a NavMesh and place the NPC on it.", this);
                    warnedAboutNavMesh = true;
                }

                destinationCheckTimer -= Time.deltaTime;
                if (destinationCheckTimer <= 0f)
                {
                    destinationCheckTimer = 1f;
                    ChooseNextDestination();
                }

                return;
            }

            warnedAboutNavMesh = false;

            destinationCheckTimer -= Time.deltaTime;
            if (destinationCheckTimer > 0f)
            {
                return;
            }

            destinationCheckTimer = DestinationCheckInterval;

            if (agent.pathPending)
            {
                return;
            }

            bool reachedDestination = agent.remainingDistance <= agent.stoppingDistance;
            bool stopped = !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;

            if (reachedDestination && stopped)
            {
                waitTimer -= DestinationCheckInterval;
                if (waitTimer <= 0f)
                {
                    ChooseNextDestination();
                }
            }
            else
            {
                waitTimer = WaitAtDestination;
            }
        }

        private void ChooseNextDestination()
        {
            if (!agent.isOnNavMesh)
            {
                return;
            }

            Vector3 center = WanderCenter != null ? WanderCenter.position : transform.position;
            for (int attempt = 0; attempt < SampleAttempts; attempt++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * WanderRadius;
                randomOffset.y = 0f;
                Vector3 candidate = center + randomOffset;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
                {
                    if (!agent.SetDestination(hit.position))
                    {
                        Debug.LogWarning(name + " could not set a NavMesh destination.", this);
                    }

                    warnedAboutDestination = false;
                    break;
                }
            }

            if (!agent.hasPath && !warnedAboutDestination)
            {
                Debug.LogWarning(name + " could not find a destination on the NavMesh. Increase Wander Radius or Sample Distance.", this);
                warnedAboutDestination = true;
            }

            waitTimer = WaitAtDestination;
            destinationCheckTimer = DestinationCheckInterval;
        }

        private void OnDrawGizmos()
        {
            if (agent == null || !agent.hasPath)
            {
                return;
            }

            Vector3[] corners = agent.path.corners;
            Gizmos.color = Color.white;

            for (int cornerIndex = 0; cornerIndex < corners.Length - 1; cornerIndex++)
            {
                Gizmos.DrawLine(corners[cornerIndex], corners[cornerIndex + 1]);
            }
        }
}
