using UnityEngine;

namespace SimpleBuildingSystem
{
    public class PlacementState : BuildingState
    {
        private readonly BuildItemCost _buildItem;
        private BuildingPart _previewInstance;
        private float _currentYRotation;
        private int _placementCost;

        private BuildingSocket _currentSocket;

        public PlacementState(BuildingController controller, BuildItemCost buildItem) : base(controller)
        {
            _buildItem = buildItem;
            if (_buildItem != null)
                _placementCost = _buildItem.metalCost;
        }

        public override void Enter()
        {
            SpawnPreview();
        }

        public override void Tick()
        {
            if (_previewInstance == null) return;
            if (controller == null || controller.ActiveView == null) return;

            bool hasHit = controller.ActiveView.TryGetPlacementPoint(out RaycastHit hit);
            if (!hasHit)
            {
                _previewInstance.gameObject.SetActive(false);
                _currentSocket = null;
                return;
            }

            _previewInstance.gameObject.SetActive(true);

            Vector3 targetPos;
            Quaternion targetRot;

            _currentSocket = BuildingSocket.FindNearest(hit.point, 1f, _previewInstance);

            if (_currentSocket != null)
            {
                targetPos = _currentSocket.GetSnapPosition();
                targetRot = _currentSocket.GetSnapRotation() * Quaternion.Euler(0, _currentYRotation, 0);
            }
            else
            {
                targetPos = GetPlacementPosition(hit);
                if (controller.useGridSnap)
                    targetPos = SnapToGridXZ(targetPos, controller.snapSize);

                targetRot = Quaternion.Euler(0, _currentYRotation, 0);

                // FIX: aplicamos a rotação ANTES de medir a bounds, porque a rotação
                // altera a AABB (bounding box) do modelo. Antes, a rotação só era
                // aplicada depois de calcular o offset, por isso o cálculo de altura
                // usava sempre a rotação do frame anterior (1 frame de atraso) — normalmente
                // impercetível, mas piora com modelos assimétricos como uma drill.
                _previewInstance.transform.rotation = targetRot;

                // FIX PRINCIPAL: antes usávamos bounds.extents.y (metade da altura total),
                // o que só está correto se o pivot do prefab estiver exatamente no centro
                // vertical da malha (como acontece nos cubos). Modelos 3D importados (ex:
                // drills) quase nunca têm o pivot no centro — pode estar na base, no topo,
                // ou em qualquer sítio definido pelo artista/asset. Isso fazia a peça
                // aparecer a flutuar ou enterrada, sempre que o pivot não coincidia com o
                // centro do modelo.
                //
                // Agora medimos a distância real entre o pivot atual e a base da malha
                // (bounds.min.y), o que funciona corretamente seja qual for a posição do
                // pivot dentro do modelo.
                targetPos += Vector3.up * (GetPivotToBottomOffset() + controller.placementHeightOffset);
            }

            targetPos += _previewInstance.placementOffset;

            _previewInstance.transform.SetPositionAndRotation(targetPos, targetRot);

            bool isValid = EvaluatePlacement(_currentSocket);
            _previewInstance.SetPreviewState(isValid);
        }

        private bool EvaluatePlacement(BuildingSocket socket)
        {
            Bounds bounds = GetPreviewBounds();
            Vector3 halfExtents = bounds.extents;
            float ignoreBottom = Mathf.Min(0.15f, halfExtents.y * 0.5f);
            halfExtents.y = Mathf.Max(0.01f, halfExtents.y - ignoreBottom);
            Vector3 center = bounds.center + Vector3.up * ignoreBottom;

            if (halfExtents.y <= 0f)
                return true;

            Collider[] overlaps = Physics.OverlapBox(
                center,
                halfExtents,
                _previewInstance.transform.rotation,
                controller.obstructionMask,
                QueryTriggerInteraction.Ignore
            );

            BuildingPart neighborPart = socket != null ? socket.GetComponentInParent<BuildingPart>() : null;

            foreach (var overlap in overlaps)
            {
                if (overlap.transform.IsChildOf(_previewInstance.transform))
                    continue;

                if (neighborPart != null && overlap.transform.IsChildOf(neighborPart.transform))
                    continue;

                return false;
            }

            return true;
        }

        private Vector3 SnapToGridXZ(Vector3 position, float snapSize)
        {
            if (snapSize <= 0f)
                snapSize = 1f;

            return new Vector3(
                Mathf.Round(position.x / snapSize) * snapSize,
                position.y,
                Mathf.Round(position.z / snapSize) * snapSize
            );
        }

        private Vector3 GetPlacementPosition(RaycastHit hit)
        {
            Vector3 point = hit.point;

            if (hit.normal.y < 0.7f)
            {
                Ray down = new Ray(point + Vector3.up * 2f, Vector3.down);
                if (Physics.Raycast(down, out RaycastHit groundHit, 20f, controller.placementMask, QueryTriggerInteraction.Ignore))
                {
                    point = groundHit.point;
                }
                else
                {
                    point = hit.point;
                }
            }

            if (controller.ActiveView is FirstPersonBuildingView fpv && fpv.sourceCamera != null)
            {
                Vector3 cameraPos = fpv.sourceCamera.transform.position;
                float dist = Vector3.Distance(point, cameraPos);
                if (dist < controller.minPlacementDistance)
                {
                    point = cameraPos + fpv.sourceCamera.transform.forward * controller.minPlacementDistance;
                }
            }

            return point;
        }

        // FIX: substitui o antigo GetPreviewBottomOffset(). Em vez de assumir que o pivot
        // está no centro do modelo (bounds.extents.y), medimos a distância vertical real
        // entre a posição atual do pivot e a base da bounding box (bounds.min.y).
        // Para um cubo com pivot no centro isto dá exatamente o mesmo valor de antes
        // (extents.y), portanto não muda nada para as peças que já funcionavam bem —
        // só corrige os casos em que o pivot está deslocado (drills e outros modelos
        // 3D complexos).
        private float GetPivotToBottomOffset()
        {
            if (_previewInstance == null)
                return 0.01f;

            Bounds bounds = GetPreviewBounds();

            // Bounds vazia (sem renderers encontrados) -> não há nada para medir.
            if (bounds.size == Vector3.zero)
                return 0.01f;

            return _previewInstance.transform.position.y - bounds.min.y;
        }

        private Bounds GetPreviewBounds()
        {
            // FIX: incluímos renderers inativos (true) porque alguns prefabs (ex: LODs,
            // variantes) têm meshes desativados por defeito, e antes eram ignorados,
            // o que também podia calcular uma bounding box incompleta/errada.
            var renderers = _previewInstance.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return new Bounds(_previewInstance.transform.position, Vector3.zero);

            // FIX: a bounds deixou de começar com "semente" na posição do pivot (que podia
            // estar longe da malha e inflacionar artificialmente a bounding box). Agora
            // começa a partir do primeiro renderer encontrado, e só depois inclui os
            // restantes — isto dá sempre a bounding box real do modelo, independentemente
            // de onde o pivot foi colocado.
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        public override void OnConfirm()
        {
            if (_previewInstance == null) return;
            if (controller == null || controller.ActiveView == null) return;
            if (!controller.ActiveView.TryGetPlacementPoint(out _)) return;

            if (!EvaluatePlacement(_currentSocket))
            {
                Debug.LogWarning("Não é possível colocar aqui. A posição é inválida.");
                return;
            }

            int cost = _placementCost;
            if (cost > 0)
            {
                if (InventoryManager.Instance == null || !InventoryManager.Instance.HasResource("Metal", cost))
                {
                    Debug.LogWarning("Não tens Metal suficiente para colocar esta peça.");
                    return;
                }
                InventoryManager.Instance.RemoveResource("Metal", cost);
            }

            _previewInstance.OnPlaced();

            if (_currentSocket != null)
            {
                _currentSocket.Connect(_previewInstance);
                _previewInstance.attachedToSocket = _currentSocket;
            }

            if (BuildingManager.Instance != null)
                BuildingManager.Instance.RegisterPart(_previewInstance);
            else
                Debug.LogWarning("BuildingManager not present: placed part won't be tracked.");

            SpawnPreview();
        }

        public override void OnCancel()
        {
            controller.SwitchToIdle();
        }

        public override void OnRotate(float direction)
        {
            if (_previewInstance == null) return;
            _currentYRotation += direction * _previewInstance.rotationStep;
        }

        public override void Exit()
        {
            _currentSocket = null;
            if (_previewInstance != null)
                Object.Destroy(_previewInstance.gameObject);
        }

        private void SpawnPreview()
        {
            if (_buildItem == null || _buildItem.prefab == null)
            {
                Debug.LogWarning("PlacementState: prefab não definido no item de construção.");
                return;
            }

            var go = Object.Instantiate(_buildItem.prefab);
            _previewInstance = go.GetComponent<BuildingPart>();
            if (_previewInstance == null)
            {
                _previewInstance = go.AddComponent<BuildingPart>();
                Debug.Log("PlacementState: prefab não tinha BuildingPart, adicionando componente de runtime.");
            }

            _previewInstance.isPreview = true;
            _previewInstance.metalCost = _placementCost;

            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.isTrigger = true;
        }
    }
}