using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int metal = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    public void AddMetal(int amount)
    {
        metal += amount;
    }
}