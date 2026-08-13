using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemSO item;
    public int amount = 1;

    [Header("World Item Settings")]
    public float bobSpeed = 1.5f;
    public float bobHeight = 0.15f;
    public float rotateSpeed = 45f;
    public float pickupDelay = 0.4f;

    private Vector3 startPos;
    private float spawnTime;

    private void Awake()
    {
        MeshCollider[] meshCols = GetComponentsInChildren<MeshCollider>();
        foreach (var mc in meshCols)
        {
            if (!mc.convex)
                mc.convex = true;
        }
    }

    private void Start()
    {
        startPos = transform.position;
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (item == null) return;

        float elapsed = Time.time - spawnTime;
        float yOffset = Mathf.Sin(elapsed * bobSpeed) * bobHeight;
        transform.position = startPos + Vector3.up * yOffset;
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    public bool CanPickup()
    {
        return Time.time - spawnTime >= pickupDelay;
    }
}
