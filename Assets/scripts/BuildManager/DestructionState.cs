using UnityEngine;

namespace SimpleBuildingSystem
{
    public class DestructionState : BuildingState
    {
        private BuildingPart _highlighted;

        public DestructionState(BuildingController controller) : base(controller) { }

        public override void Tick()
        {
            if (controller == null || controller.ActiveView == null) return;
            if (!controller.ActiveView.TryGetPlacementPoint(out RaycastHit hit)) 
            {
                ClearHighlight();
                return;
            }

            var part = hit.collider.GetComponentInParent<BuildingPart>();
            if (part == null || !part.isPlaced)
            {
                ClearHighlight();
                return;
            }

            // FIX (WebGL): antes isto fazia restore + setPreview em TODOS os frames,
            // mesmo sem mudar de peca. Como ambos os caminhos escreviam em
            // Renderer.materials (que instancia materiais), isso vazava nativo
            // continuamente e rebentava a tab. Agora so trabalha na transicao.
            if (part == _highlighted)
                return;

            ClearHighlight();
            _highlighted = part;
            _highlighted.SetPreviewState(false); // usa o material "inválido" como highlight de destruição
        }

        private void ClearHighlight()
        {
            if (_highlighted == null)
            {
                _highlighted = null;
                return;
            }

            _highlighted.RestoreOriginalMaterials();
            _highlighted = null;
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
            ClearHighlight();
        }
    }
}
