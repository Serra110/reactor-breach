using UnityEngine;

namespace SimpleBuildingSystem
{
    // Classe base para qualquer "perspetiva" de raycast (primeira pessoa, terceira pessoa,
    // top-down, orbital...). Cria uma subclasse só se nenhuma das prontas te servir.
    public abstract class BuildingView : MonoBehaviour
    {
        public float maxDistance = 6f;
        public LayerMask placementMask = ~0; // por defeito, colide com tudo
        public LayerMask ignoreMask = 0;
        public bool ignorePlayerTaggedObjects = true;

        protected bool ShouldIgnoreCollider(Collider collider)
        {
            if (collider == null) return false;

            if ((ignoreMask & (1 << collider.gameObject.layer)) != 0)
                return true;

            if (ignorePlayerTaggedObjects && collider.CompareTag("Player"))
                return true;

            if (collider.transform.IsChildOf(transform))
                return true;

            Camera mainCamera = Camera.main;
            if (mainCamera != null && collider.transform.IsChildOf(mainCamera.transform))
                return true;

            return false;
        }

        public abstract bool TryGetPlacementPoint(out RaycastHit hit);
    }
}
