using UnityEngine;

namespace SimpleBuildingSystem
{
    // Lança um ray a partir do centro do ecrã (câmara em terceira pessoa).
    // Se quiseres primeira pessoa, a lógica é igual — muda só a câmara de referência.
    public class ThirdPersonBuildingView : BuildingView
    {
        [Tooltip("Câmara a partir da qual o ray é lançado. Se vazio, usa Camera.main.")]
        public Camera sourceCamera;

        private void Awake()
        {
            if (sourceCamera == null) sourceCamera = Camera.main;
        }

        public override bool TryGetPlacementPoint(out RaycastHit hit)
        {
            hit = default;
            if (sourceCamera == null) return false;

            Ray ray = sourceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
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
