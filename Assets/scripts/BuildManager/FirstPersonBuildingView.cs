using UnityEngine;

namespace SimpleBuildingSystem
{
    // Simple first-person view that raycasts from the camera forward.
    public class FirstPersonBuildingView : BuildingView
    {
        public Camera sourceCamera;

        // FIX (WebGL): RaycastAll aloca um RaycastHit[] novo por frame e o Array.Sort
        // com lambda aloca um delegate por frame. Em WebGL isso satura o GC e provoca
        // pausas. RaycastNonAlloc + procura manual do mais proximo nao aloca nada.
        private const int MaxHits = 32;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[MaxHits];

        private void Awake()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;
        }

        public override bool TryGetPlacementPoint(out RaycastHit hit)
        {
            hit = default;
            if (sourceCamera == null) return false;

            Vector3 origin = sourceCamera.transform.position;
            Vector3 direction = sourceCamera.transform.forward;
            Ray ray = new Ray(origin, direction);
            int count = Physics.RaycastNonAlloc(ray, _hitBuffer, maxDistance, placementMask, QueryTriggerInteraction.Ignore);
            if (count == 0) return false;

            float bestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = _hitBuffer[i];
                if (candidate.distance >= bestDistance)
                    continue;
                if (ShouldIgnoreCollider(candidate.collider))
                    continue;

                bestDistance = candidate.distance;
                hit = candidate;
                found = true;
            }

            return found;
        }
    }
}
