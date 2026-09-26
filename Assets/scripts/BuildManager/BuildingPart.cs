using System.Collections.Generic;
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

        // FIX (WebGL): array de trabalho reutilizado. Atribuir a Renderer.materials
        // instancia um Material novo por renderer, por frame, e o antigo nunca e
        // destruido -> vazamento nativo que acaba em OOM/crash do browser.
        // SetSharedMaterials substitui a lista sem clonar, entao nao aloca nada
        // (embora a API exija List<Material>, a lista e reutilizada).
        private List<Material>[] _scratchMaterials;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalMaterials = new Material[_renderers.Length][];
            _previewMaterials = new Material[_renderers.Length][];
            _scratchMaterials = new List<Material>[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                // sharedMaterials nao instanciam copias.
                _originalMaterials[i] = _renderers[i].sharedMaterials;
                _previewMaterials[i] = new Material[_originalMaterials[i].Length];
                _scratchMaterials[i] = new List<Material>(_originalMaterials[i].Length);
            }
        }

        private void OnDestroy()
        {
            if (_previewMaterials == null)
                return;

            for (int i = 0; i < _previewMaterials.Length; i++)
            {
                Material[] preview = _previewMaterials[i];
                if (preview == null)
                    continue;
                for (int j = 0; j < preview.Length; j++)
                {
                    if (preview[j] != null)
                        Destroy(preview[j]);
                    preview[j] = null;
                }
            }
        }

        /// <summary>
        /// Troca a lista de materiais do renderer sem instanciar copias nativas.
        /// Nao escreve nos materiais partilhados, apenas substitui a referencia.
        /// </summary>
        private void ApplyMaterialSet(int rendererIndex, Material[] source)
        {
            Renderer target = _renderers[rendererIndex];
            if (target == null)
                return;

            List<Material> scratch = _scratchMaterials[rendererIndex];
            if (scratch.Count != source.Length)
                scratch.Capacity = source.Length;

            bool changed = false;
            for (int j = 0; j < source.Length; j++)
            {
                if (j < scratch.Count)
                {
                    if (scratch[j] != source[j])
                    {
                        scratch[j] = source[j];
                        changed = true;
                    }
                }
                else
                {
                    scratch.Add(source[j]);
                    changed = true;
                }
            }

            while (scratch.Count > source.Length)
            {
                scratch.RemoveAt(scratch.Count - 1);
                changed = true;
            }

            // Só chama a API quando algo mudou mesmo, para não marcar o renderer
            // como sujo em todos os frames.
            if (changed)
                target.SetSharedMaterials(scratch);
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
                    ApplyMaterialSet(i, mats);
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
                Material[] originals = _originalMaterials[i];
                Material[] mats = _previewMaterials[i];
                for (int j = 0; j < mats.Length; j++)
                {
                    if (mats[j] == null)
                        mats[j] = CreatePreviewCopy(originals[j]);

                    var pm = mats[j];
                    if (pm.HasProperty("_BaseColor"))
                        pm.SetColor("_BaseColor", tint);
                    if (pm.HasProperty("_Color"))
                        pm.SetColor("_Color", tint);
                }
                ApplyMaterialSet(i, mats);
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
                ApplyMaterialSet(i, _originalMaterials[i]);
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