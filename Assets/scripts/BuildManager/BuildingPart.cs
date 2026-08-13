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
        private Material[][] _previewMaterials;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalMaterials = new Material[_renderers.Length][];
            _previewMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalMaterials[i] = _renderers[i].materials;
                _previewMaterials[i] = new Material[_renderers[i].materials.Length];
            }
        }

        public void SetPreviewState(bool valid)
        {
            Material mat = valid ? previewMaterialValid : previewMaterialInvalid;
            if (mat != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    Material[] mats = _previewMaterials[i];
                    for (int j = 0; j < mats.Length; j++) mats[j] = mat;
                    _renderers[i].materials = mats;
                }
                return;
            }

            ApplyPreviewColor(valid ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f));
        }

        // FIX: fallback automático quando o prefab não tem previewMaterialValid/Invalid
        // atribuídos. Cria uma cópia translúcida do material original do renderer (verde
        // = válido, vermelho = inválido), funcionando tanto em URP como no pipeline
        // clássico.
        private void ApplyPreviewColor(Color tint)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material[] mats = _renderers[i].materials;
                for (int j = 0; j < mats.Length; j++)
                {
                    if (_previewMaterials[i][j] == null)
                        _previewMaterials[i][j] = CreatePreviewCopy(mats[j]);

                    var pm = _previewMaterials[i][j];
                    if (pm.HasProperty("_BaseColor"))
                        pm.SetColor("_BaseColor", tint);
                    if (pm.HasProperty("_Color"))
                        pm.SetColor("_Color", tint);
                }
                _renderers[i].materials = _previewMaterials[i];
            }
        }

        private Material CreatePreviewCopy(Material original)
        {
            Material clone = new Material(original);
            clone.name = original.name + " (Preview)";
            clone.SetOverrideTag("RenderType", "Transparent");
            clone.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            clone.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            clone.SetInt("_ZWrite", 0);
            clone.DisableKeyword("_ALPHATEST_ON");
            clone.EnableKeyword("_ALPHABLEND_ON");
            clone.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (clone.HasProperty("_Surface"))
                clone.SetFloat("_Surface", 1f);
            clone.renderQueue = 3000;
            return clone;
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