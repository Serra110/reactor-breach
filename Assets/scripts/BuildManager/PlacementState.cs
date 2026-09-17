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
        private Renderer[] _cachedRenderers;
        private float _noHitLogTimer;
        private float _diagTimer;

        public PlacementState(BuildingController controller, BuildItemCost buildItem) : base(controller)
        {
            _buildItem = buildItem;
            if (_buildItem != null)
                _placementCost = _buildItem.metalCost;
        }

        public override void Enter()
        {
            if (DebugFlags.buildLogs) Debug.Log($"[PlacementState] Enter | item={(_buildItem == null ? "NULL" : (_buildItem.prefab == null ? "prefabNULL" : _buildItem.prefab.name))} | cost={_placementCost}");
            SpawnPreview();
        }

        public override void Tick()
        {
            if (_previewInstance == null) return;
            if (controller == null || controller.ActiveView == null)
            {
                Debug.LogWarning("[PlacementState] controller/ActiveView null");
                return;
            }

            bool hasHit = controller.ActiveView.TryGetPlacementPoint(out RaycastHit hit);
            if (!hasHit)
            {
                _noHitLogTimer -= Time.deltaTime;
                if (_noHitLogTimer <= 0f)
                {
                    _noHitLogTimer = 1f;
                    if (DebugFlags.buildLogs) Debug.Log("[PlacementState] NO HIT - escondendo preview (sem superficie a <8m)");
                }
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
                // FIX: suporte a montagem em parede. Se o raycast atinge uma superfície
                // vertical (normal não aponta para cima), o objeto é orientado para
                // "olhar" para fora da parede e fica encostado à superfície. Antes, tudo
                // era puxado para o chão, pelo que switch/luzes de parede nunca apareciam
                // onde o jogador olhava.
                bool isWallMount = hit.normal.y < 0.7f;

                if (isWallMount)
                {
                    Vector3 normal = hit.normal;
                    Quaternion baseRot = Quaternion.LookRotation(normal, Vector3.up);

                    // FIX: escolhe a rotação que achata o objeto contra a parede. O
                    // LookRotation(normal) só fica certo se o eixo +Z do modelo for o que
                    // deve apontar para fora — mas muitos modelos (luzes, switches) têm o
                    // eixo "para fora" noutro sentido e ficavam de lado. Testamos as 4
                    // rotações em torno da normal da parede e escolhemos a que torna o
                    // objeto mais fino na direção perpendicular (a correta para objetos
                    // de parede: a face fica encostada à parede, a frente para fora).
                    Quaternion bestRot = baseRot;
                    float bestDepth = float.MaxValue;
                    for (int k = 0; k < 4; k++)
                    {
                        Quaternion cand = baseRot * Quaternion.Euler(0f, 90f * k, 0f);
                        _previewInstance.transform.rotation = cand;
                        Bounds b = GetPreviewBounds();
                        if (b.size == Vector3.zero)
                        {
                            bestRot = cand;
                            break;
                        }
                        float depth = Mathf.Abs(Vector3.Dot(b.size, normal));
                        if (depth < bestDepth)
                        {
                            bestDepth = depth;
                            bestRot = cand;
                        }
                    }

                    targetRot = bestRot * Quaternion.Euler(0f, _currentYRotation, 0f);
                    _previewInstance.transform.rotation = targetRot;

                    float rearOffset = GetRearOffsetAlong(normal);
                    targetPos = hit.point + normal * (controller.placementHeightOffset + 0.02f - rearOffset);
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
            }

            targetPos += _previewInstance.placementOffset;

            _previewInstance.transform.SetPositionAndRotation(targetPos, targetRot);

            bool isValid = EvaluatePlacement(_currentSocket);
            _previewInstance.SetPreviewState(isValid);

            _diagTimer -= Time.unscaledDeltaTime;
            if (_diagTimer <= 0f)
            {
                _diagTimer = 0.5f;
                if (DebugFlags.buildLogs) Debug.Log($"[PlacementState] POS hit={hit.point} target={targetPos} valid={isValid}");
            }
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

        // Distância do pivot à face traseira do objeto medida ao longo da normal da
        // parede. A face traseira é o ponto do bounds com MENOR projeção na direção da
        // normal (a parte que fica encostada à parede). Se o pivot estiver fora do
        // bounds, o valor é tratado pelo chamador através de (offset - rearOffset).
        private float GetRearOffsetAlong(Vector3 normal)
        {
            Bounds bounds = GetPreviewBounds();
            if (bounds.size == Vector3.zero)
                return 0f;

            Vector3 pivot = _previewInstance.transform.position;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            float minProj = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z
                );
                minProj = Mathf.Min(minProj, Vector3.Dot(corner - pivot, normal));
            }

            return minProj;
        }

        private Bounds GetPreviewBounds()
        {
            if (_cachedRenderers == null || _cachedRenderers.Length == 0)
                return new Bounds(_previewInstance.transform.position, Vector3.zero);

            Bounds bounds = _cachedRenderers[0].bounds;
            for (int i = 1; i < _cachedRenderers.Length; i++)
                bounds.Encapsulate(_cachedRenderers[i].bounds);

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
                var inv = ReactorBreach.InventorySystem.Inventory.Instance;

                if (_buildItem != null && _buildItem.costItem != null && inv != null)
                {
                    if (!inv.HasItem(_buildItem.costItem, cost))
                    {
                        Debug.LogWarning("Não tens " + _buildItem.costItem.itemName + " suficiente para colocar esta peça.");
                        return;
                    }
                    inv.RemoveItem(_buildItem.costItem, cost);
                }
                else if (InventoryManager.Instance == null || !InventoryManager.Instance.HasResource("Metal", cost))
                {
                    Debug.LogWarning("Não tens Metal suficiente para colocar esta peça.");
                    return;
                }
                else
                {
                    InventoryManager.Instance.RemoveResource("Metal", cost);
                }
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
            }

            _previewInstance.isPreview = true;
            _previewInstance.metalCost = _placementCost;

            // FIX: enquanto é preview, todos os colliders são triggers. Assim o raycast
            // (QueryTriggerInteraction.Ignore) nunca acerta no próprio ghost — sem isto,
            // objetos grandes (ex: generator) ficavam "presos" no sítio onde nasciam
            // porque o raycast atingia os colliders deles próprios em vez do chão.
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
                col.isTrigger = true;

            _cachedRenderers = go.GetComponentsInChildren<Renderer>(true);
            if (DebugFlags.buildLogs) Debug.Log($"[PlacementState] Preview spawned: {go.name} | renderers={(_cachedRenderers == null ? 0 : _cachedRenderers.Length)}");
        }
    }
}