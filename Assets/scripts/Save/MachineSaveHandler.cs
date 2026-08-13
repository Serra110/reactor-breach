using UnityEngine;

public class MachineSaveHandler : MonoBehaviour, ISaveable, IPlaceable
{
    [SerializeField] private string machineType; // preenches no Inspector, ex: "MiningDrill"
    private string machineId;

    private void Awake()
    {
        if (string.IsNullOrEmpty(machineId))
            machineId = System.Guid.NewGuid().ToString();
    }

    private void OnEnable()
    {
        SaveManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.Unregister(this);
    }

    public string GetUniqueId() => machineId;
    public string GetMachineType() => machineType;

    
    public void AssignSavedId(string savedId)
    {
        SaveManager.Instance.Unregister(this); 
        machineId = savedId;
        SaveManager.Instance.Register(this);   
    }

    public string CaptureState()
    {
        return "{}";
    }

    public void RestoreState(string json)
    {
        
    }
}