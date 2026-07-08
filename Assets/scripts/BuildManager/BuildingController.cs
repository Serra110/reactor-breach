using UnityEngine;

namespace SimpleBuildingSystem
{
    // Liga a View ativa, o State ativo e a Part selecionada.
    // É o único ponto de entrada que o Input e a UI precisam de conhecer.
    [RequireComponent(typeof(BuildingInput))]
    public class BuildingController : MonoBehaviour
    {
        [Header("View ativa (arrasta o objeto com ThirdPersonBuildingView, por exemplo)")]
        public BuildingView ActiveView;

        [Header("Build Items + Costs")]
        public BuildItemCost[] buildItems;
        public int currentBuildIndex = 0;

        [Header("Placement Settings")]
        public float buildDistance = 8f;
        public float snapSize = 1f;
        public bool useGridSnap = true;
        public float placementHeightOffset = 0.02f;
        public LayerMask placementMask = ~0;
        public LayerMask obstructionMask = ~0;
        public LayerMask ignoreMask = 0;
        public float minPlacementDistance = 0.5f;

        private bool _placementMode = false;

        public BuildingState CurrentState { get; private set; }

        private void Start()
        {
            // ensure we have an ActiveView; prefer existing, else try to find or create a first-person view
            if (ActiveView == null)
            {
                ActiveView = FindObjectOfType<BuildingView>();
                if (ActiveView == null)
                {
                    var go = new GameObject("FirstPersonBuildingView");
                    var fp = go.AddComponent<FirstPersonBuildingView>();
                    fp.sourceCamera = Camera.main;
                    fp.placementMask = placementMask;
                    fp.ignoreMask = ignoreMask;
                    fp.ignorePlayerTaggedObjects = true;
                    fp.maxDistance = buildDistance;
                    ActiveView = fp;
                }
            }
            else
            {
                ActiveView.placementMask = placementMask;
                ActiveView.ignoreMask = ignoreMask;
                ActiveView.maxDistance = buildDistance;
            }

            SwitchToIdle();
        }

        private void Update()
        {
            CurrentState?.Tick();
        }

        public void RequestPlacementMode()
        {
            if (buildItems == null || buildItems.Length == 0)
            {
                Debug.LogWarning("[BuildingController] Nenhum item de construção definido.");
                return;
            }

            if (currentBuildIndex < 0 || currentBuildIndex >= buildItems.Length)
                currentBuildIndex = 0;

            var item = buildItems[currentBuildIndex];
            if (item == null || item.prefab == null)
            {
                Debug.LogWarning("[BuildingController] Item selecionado inválido.");
                return;
            }

            _placementMode = true;
            ChangeState(new PlacementState(this, item));
        }

        public void RequestDestructionMode()
        {
            ChangeState(new DestructionState(this));
        }

        public void SwitchToIdle()
        {
            _placementMode = false;
            ChangeState(null);
        }

        public void TogglePlacementMode()
        {
            if (_placementMode)
                SwitchToIdle();
            else
                RequestPlacementMode();
        }

        // Chama isto a partir de um menu/UI para trocar a peça selecionada.
        public void SelectBuildItem(int index)
        {
            if (buildItems == null || buildItems.Length == 0)
                return;

            currentBuildIndex = Mathf.Clamp(index, 0, buildItems.Length - 1);
        }

        public void SelectNextBuildItem(int direction)
        {
            if (buildItems == null || buildItems.Length == 0)
                return;

            currentBuildIndex = (currentBuildIndex + direction) % buildItems.Length;
            if (currentBuildIndex < 0)
                currentBuildIndex += buildItems.Length;
        }

        public BuildItemCost GetCurrentBuildItem()
        {
            if (buildItems == null || buildItems.Length == 0)
                return null;
            return buildItems[currentBuildIndex];
        }

        private void ChangeState(BuildingState newState)
        {
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter();
        }
    }
}
