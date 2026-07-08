using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int metal = 0;

    void Awake()
    {
        Instance = this;
    }

    public void AddMetal(int amount)
    {
        metal += amount;
        Debug.Log("Metal: " + metal);
    }

    public bool SpendMetal(int amount)
    {
        if (metal >= amount)
        {
            metal -= amount;
            Debug.Log("Spent metal. Remaining: " + metal);
            return true;
        }

        Debug.Log("Not enough metal!");
        return false;
    }
}