using UnityEngine;

namespace SimpleBuildingSystem
{
    [DisallowMultipleComponent]
    public class BlockPreview : MonoBehaviour
    {
        public Material previewMaterialValid;
        public Material previewMaterialInvalid;

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
            if (_renderers == null || _renderers.Length == 0)
                Awake();

            Material previewMaterial = valid ? previewMaterialValid : previewMaterialInvalid;
            if (previewMaterial == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Material[] mats = _previewMaterials[i];
                for (int j = 0; j < mats.Length; j++)
                    mats[j] = previewMaterial;
                _renderers[i].materials = mats;
            }
        }

        public void RestoreOriginalMaterials()
        {
            if (_renderers == null || _originalMaterials == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                if (_originalMaterials[i] == null)
                    continue;

                _renderers[i].materials = _originalMaterials[i];
            }
        }

        private void OnDestroy()
        {
            RestoreOriginalMaterials();
        }
    }
}
