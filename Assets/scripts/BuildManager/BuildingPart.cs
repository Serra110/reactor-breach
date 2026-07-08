using UnityEngine;

namespace SimpleBuildingSystem
{
    // O "átomo" do sistema: qualquer objeto colocável tem este componente.
    // Guarda o preview visual, o estado (preview/colocada) e a lógica de destruição.
    [RequireComponent(typeof(Collider))]
    public class BuildingPart : MonoBehaviour
    {
        [Header("Identificação")]
        public string partId = "part_default";

        [Header("Cost")]
        [Tooltip("Metal cost to place this part. 0 = free")] public int metalCost = 0;

        [Header("Preview / Renderer")]
        public Material previewMaterialValid;
        public Material previewMaterialInvalid;

        [Header("Placement")]
        public float rotationStep = 15f;
        public Vector3 placementOffset = Vector3.zero;

        [HideInInspector] public bool isPreview = false;
        [HideInInspector] public bool isPlaced = false;

        // FIX: guarda a que socket esta peça está ligada, para o podermos libertar quando
        // a peça for destruída. Sem isto, um socket ficava marcado como "ocupado" para
        // sempre depois de a peça ligada a ele ser destruída, e mais nenhuma peça
        // conseguia fazer snap ali (parecia um "buraco" permanente).
        [HideInInspector] public BuildingSocket attachedToSocket;

        private Renderer[] _renderers;
        private Material[][] _originalMaterials;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
                _originalMaterials[i] = _renderers[i].materials;
        }

        // Pinta a peça a verde/vermelho consoante a colocação seja válida ou não.
        public void SetPreviewState(bool valid)
        {
            Material mat = valid ? previewMaterialValid : previewMaterialInvalid;
            if (mat == null) return;

            foreach (var r in _renderers)
            {
                Material[] mats = new Material[r.materials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.materials = mats;
            }
        }

        public void RestoreOriginalMaterials()
        {
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].materials = _originalMaterials[i];
        }

        // Chamado quando o jogador confirma a colocação.
        public void OnPlaced()
        {
            isPreview = false;
            isPlaced = true;
            RestoreOriginalMaterials();

            foreach (var col in GetComponentsInChildren<Collider>())
                col.isTrigger = false;
        }

        // Chamado quando o jogador destrói a peça no modo Destruction.
        public void OnDestroyedByPlayer()
        {
            isPlaced = false;

            // FIX: liberta o socket a que estava ligada, para outra peça poder ocupá-lo.
            if (attachedToSocket != null)
            {
                attachedToSocket.Disconnect();
                attachedToSocket = null;
            }

            BuildingManager.Instance?.UnregisterPart(this);
            Destroy(gameObject);
        }
    }
}