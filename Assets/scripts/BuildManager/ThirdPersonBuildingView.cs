using UnityEngine;

namespace SimpleBuildingSystem
{
    // Lança um ray a partir do centro do ecrã (câmara em terceira pessoa).
    // Se quiseres primeira pessoa, a lógica é igual — muda só a câmara de referência.
    public class ThirdPersonBuildingView : BuildingView
    {
        [Tooltip("Câmara a partir da qual o ray é lançado. Se vazio, usa Camera.main.")]
        public Camera sourceCamera;

        // FIX (WebGL): ver FirstPersonBuildingView — RaycastAll + Array.Sort(lambda)
        // alocavam por frame. RaycastNonAlloc + minima manual nao aloca.
        private const int MaxHits = 32;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[MaxHits];

        private void Awake()
        {
            if (sourceCamera == null) sourceCamera = Camera.main;
        }

        public override bool TryGetPlacementPoint(out RaycastHit hit)
        {
            hit = default;
            if (sourceCamera == null) return false;

            Ray ray = sourceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
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
