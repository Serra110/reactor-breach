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

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalMaterials[i] = _renderers[i].materials;
            }
        }

        public void SetPreviewState(bool valid)
        {
            if (_renderers == null || _renderers.Length == 0)
                Awake();

            Material previewMaterial = valid ? previewMaterialValid : previewMaterialInvalid;
            if (previewMaterial == null)
                return;

            foreach (var renderer in _renderers)
            {
                Material[] mats = new Material[renderer.materials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = previewMaterial;
                renderer.materials = mats;
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
