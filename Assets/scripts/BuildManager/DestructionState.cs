using UnityEngine;

namespace SimpleBuildingSystem
{
    public class DestructionState : BuildingState
    {
        private BuildingPart _highlighted;

        public DestructionState(BuildingController controller) : base(controller) { }

        public override void Tick()
        {
            if (_highlighted != null)
            {
                _highlighted.RestoreOriginalMaterials();
                _highlighted = null;
            }

            if (controller == null || controller.ActiveView == null) return;
            if (!controller.ActiveView.TryGetPlacementPoint(out RaycastHit hit)) return;

            var part = hit.collider.GetComponentInParent<BuildingPart>();
            if (part != null && part.isPlaced)
            {
                _highlighted = part;
                part.SetPreviewState(false); // usa o material "inválido" como highlight de destruição
            }
        }

        public override void OnConfirm()
        {
            if (_highlighted == null) return;
            _highlighted.OnDestroyedByPlayer();
            _highlighted = null;
        }

        public override void OnCancel()
        {
            controller.SwitchToIdle();
        }

        public override void Exit()
        {
            if (_highlighted != null)
                _highlighted.RestoreOriginalMaterials();
        }
    }
}
