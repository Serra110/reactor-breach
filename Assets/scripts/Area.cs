using UnityEngine;
using UnityEngine.AI;

public class Area : MonoBehaviour
    {
        public float Radius = 20f;

        [Tooltip("Number of attempts used to find a point on the NavMesh.")]
        [Min(1)]
        public int SampleAttempts = 10;

        [Tooltip("Maximum distance from a random point to the NavMesh.")]
        [Min(0f)]
        public float NavMeshSampleDistance = 2f;

        public Vector3 GetRandomPoint()
        {
            for (int attempt = 0; attempt < SampleAttempts; attempt++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * Radius;
                randomOffset.y = 0f;
                Vector3 candidate = transform.position + randomOffset;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
}
