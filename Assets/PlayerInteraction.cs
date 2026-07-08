using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                Ore ore = hit.collider.GetComponent<Ore>();

                if (ore != null)
                {
                    GameManager.Instance.AddMetal(ore.metalAmount);
                    Destroy(hit.collider.gameObject);
                }
            }
        }
    }
}