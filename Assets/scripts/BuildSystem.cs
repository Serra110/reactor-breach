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

    [Header("Settings")]
    public float buildDistance = 5f;

    [Header("Input Keys")]
    public KeyCode toggleBuildKey = KeyCode.B;
    public KeyCode rotateKey = KeyCode.R;
    public KeyCode deleteKey = KeyCode.X;

    private bool buildMode = false;
    private GameObject ghostObject;

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

        GameObject obj = Instantiate(
            item.prefab,
            ghostObject.transform.position,
            ghostObject.transform.rotation
        );

        // 🔥 ATIVA O SISTEMA DE PLACEMENT
        MiningDrill drill = obj.GetComponent<MiningDrill>();
        if (drill != null)
        {
            drill.SetPlaced();
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

        // ❌ DESATIVAR COLISORES NO GHOST
        Collider[] cols = ghostObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in cols)
            col.enabled = false;

        // ❌ GARANTIR QUE NADA PRODUZ NO GHOST
        MiningDrill drill = ghostObject.GetComponent<MiningDrill>();
        if (drill != null)
        {
            drill.enabled = false;
        }
    }

    void UpdateGhost()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(ray, out RaycastHit hit, buildDistance))
        {
            Vector3 pos = hit.point;

            pos.x = Mathf.Round(pos.x);
            pos.z = Mathf.Round(pos.z);

            Renderer rend = ghostObject.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                pos.y = hit.point.y + rend.bounds.extents.y;
            }
            else
            {
                pos.y = hit.point.y;
            }

            ghostObject.transform.position = pos;
        }
    }
}