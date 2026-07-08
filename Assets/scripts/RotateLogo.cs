using UnityEngine;

public class RotateLogo : MonoBehaviour
{
    public float speed = 50f;

    void Update()
    {
        transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}