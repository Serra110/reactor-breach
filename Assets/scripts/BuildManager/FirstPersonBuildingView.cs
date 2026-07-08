using UnityEngine;

namespace SimpleBuildingSystem
{
    // Simple first-person view that raycasts from the camera forward.
    public class FirstPersonBuildingView : BuildingView
    {
        public Camera sourceCamera;

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
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, placementMask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var candidate in hits)
            {
                if (ShouldIgnoreCollider(candidate.collider))
                    continue;

                hit = candidate;
                return true;
            }

            return false;
        }
    }
}
