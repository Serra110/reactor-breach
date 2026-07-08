using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildItemCost
{
    public GameObject prefab;
    public int metalCost;
}

public class BuildSystem : MonoBehaviour
{
    [Header("Build Objects + Costs")]
    public BuildItemCost[] buildItems;
    private int currentPrefabIndex = 0;

    [Header("References")]
    public Camera playerCamera;

    [Header("Placement Settings")]
    public float buildDistance = 8f;
    public float snapSize = 1f;
    public float placementOffset = 0.02f;
    public LayerMask placementMask = -1;
    public LayerMask obstructionMask = -1;
    public bool useGridSnap = true;

    [Header("Preview Materials")]
    public Material previewMaterialValid;
    public Material previewMaterialInvalid;

    [Header("Input Keys")]
    public KeyCode toggleBuildKey = KeyCode.B;
    public KeyCode rotateKey = KeyCode.R;
    public KeyCode deleteKey = KeyCode.X;

    private bool buildMode = false;
    private GameObject ghostObject;
    private Renderer[] ghostRenderers;
    private Material[][] ghostOriginalMaterials;
    private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

    void Update()
    {
        if (Input.GetKeyDown(toggleBuildKey))
        {
            buildMode = !buildMode;

            if (buildMode)
                CreateGhost();
            else if (ghostObject != null)
                Destroy(ghostObject);
        }

        if (!buildMode || ghostObject == null)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            currentPrefabIndex++;
            if (currentPrefabIndex >= buildItems.Length)
                currentPrefabIndex = 0;

            CreateGhost();
        }
        else if (scroll < 0f)
        {
            currentPrefabIndex--;
            if (currentPrefabIndex < 0)
                currentPrefabIndex = buildItems.Length - 1;

            CreateGhost();
        }

        UpdateGhost();

        if (Input.GetKeyDown(rotateKey))
            ghostObject.transform.Rotate(0, 90, 0);

        if (Input.GetMouseButtonDown(0))
            TryPlaceObject();

        if (Input.GetKeyDown(deleteKey))
            TryDeleteObject();
    }

    void TryPlaceObject()
    {
        BuildItemCost item = buildItems[currentPrefabIndex];

        if (item.prefab == null)
            return;

        if (InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.RemoveResource("Metal", item.metalCost))
            {
                Debug.Log("❌ Sem Metal suficiente, pobre coitado.");
                return;
            }
        }

        if (!CanPlaceAt(ghostObject.transform.position))
        {
            Debug.Log("⚠️ Não é possível colocar aqui. Espaço ocupado ou inválido.");
            return;
        }

        GameObject obj = Instantiate(
            item.prefab,
            ghostObject.transform.position,
            ghostObject.transform.rotation
        );

        occupiedCells.Add(WorldToCell(ghostObject.transform.position));

        // 🔥 ATIVA O SISTEMA DE PLACEMENT
        DrillMachine drill = obj.GetComponent<DrillMachine>();
        if (drill != null)
        {
            drill.SetPlaced();
            Debug.Log("✅ Drill placed at: " + obj.transform.position);
        }
        else
        {
            Debug.LogError("❌ DrillMachine component not found on placed object!");
        }
    }

    void TryDeleteObject()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(ray, out RaycastHit hit, buildDistance))
        {
            if (hit.collider.gameObject != ghostObject)
            {
                Destroy(hit.collider.gameObject);
                Debug.Log("🗑 Objeto removido (economia de demolição)");
            }
        }
    }

    void CreateGhost()
    {
        if (ghostObject != null)
            Destroy(ghostObject);

        GameObject prefab = buildItems[currentPrefabIndex].prefab;

        ghostObject = Instantiate(prefab);
        ghostRenderers = ghostObject.GetComponentsInChildren<Renderer>();
        ghostOriginalMaterials = new Material[ghostRenderers.Length][];
        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            ghostOriginalMaterials[i] = ghostRenderers[i].materials;
        }

        SetGhostPreviewState(false);

        // ❌ DESATIVAR COLISORES NO GHOST
        Collider[] cols = ghostObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in cols)
            col.enabled = false;

        // ❌ GARANTIR QUE NADA PRODUZ NO GHOST
        DrillMachine drill = ghostObject.GetComponent<DrillMachine>();
        if (drill != null)
        {
            drill.enabled = false;
        }
    }

    void UpdateGhost()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || ghostObject == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            buildDistance,
            placementMask,
            QueryTriggerInteraction.Ignore
        );

        Vector3 targetPosition;
        Vector3 targetNormal;

        if (hasHit)
        {
            targetPosition = hit.point;
            targetNormal = hit.normal;
        }
        else
        {
            targetPosition = ray.GetPoint(buildDistance * 0.5f);
            targetNormal = Vector3.up;
        }

        Vector3 snappedPosition = useGridSnap ? SnapToGrid(targetPosition) : targetPosition;
        float surfaceOffset = GetGhostSurfaceOffset(targetNormal);
        Vector3 finalPosition = snappedPosition + (targetNormal * (surfaceOffset + placementOffset));

        bool validLocation = CanPlaceAt(finalPosition);

        if (occupiedCells.Contains(WorldToCell(finalPosition)))
        {
            ghostObject.transform.position = finalPosition + Vector3.up * 0.02f;
        }
        else
        {
            ghostObject.transform.position = finalPosition;
        }

        SetGhostPreviewState(validLocation);
    }

    private void SetGhostPreviewState(bool valid)
    {
        if (ghostRenderers == null || ghostRenderers.Length == 0)
            return;

        Material previewMaterial = valid ? previewMaterialValid : previewMaterialInvalid;
        if (previewMaterial == null)
            return;

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            if (ghostRenderers[i] == null)
                continue;

            Material[] mats = new Material[ghostRenderers[i].materials.Length];
            for (int j = 0; j < mats.Length; j++)
                mats[j] = previewMaterial;

            ghostRenderers[i].materials = mats;
        }
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        if (snapSize <= 0f)
            snapSize = 1f;

        return new Vector3(
            Mathf.Round(position.x / snapSize) * snapSize,
            Mathf.Round(position.y / snapSize) * snapSize,
            Mathf.Round(position.z / snapSize) * snapSize
        );
    }

    Vector3Int WorldToCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.RoundToInt(position.x / snapSize),
            Mathf.RoundToInt(position.y / snapSize),
            Mathf.RoundToInt(position.z / snapSize)
        );
    }

    bool CanPlaceAt(Vector3 position)
    {
        if (snapSize <= 0f)
            snapSize = 1f;

        Vector3Int cell = WorldToCell(position);
        if (occupiedCells.Contains(cell))
            return false;

        if (obstructionMask == 0)
            return true;

        Collider[] overlaps = Physics.OverlapSphere(position, 0.2f, obstructionMask, QueryTriggerInteraction.Ignore);
        return overlaps.Length == 0;
    }

    float GetGhostSurfaceOffset(Vector3 normal)
    {
        if (ghostObject == null)
            return 0.01f;

        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return 0.01f;

        Bounds bounds = new Bounds(ghostObject.transform.position, Vector3.zero);
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);

        Vector3 n = normal.normalized;
        return Mathf.Abs(n.x) * bounds.extents.x + Mathf.Abs(n.y) * bounds.extents.y + Mathf.Abs(n.z) * bounds.extents.z;
    }
}