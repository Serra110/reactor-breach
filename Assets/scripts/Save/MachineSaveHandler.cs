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
        RegisterWithSaveManager();
    }

    private void Start()
    {
        RegisterWithSaveManager();
    }

    private void RegisterWithSaveManager()
    {
        if (SaveManager.Instance != null)
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
        if (SaveManager.Instance != null)
            SaveManager.Instance.Unregister(this);
        machineId = savedId;
        RegisterWithSaveManager();
    }

    public string CaptureState()
    {
        return "{}";
    }

    public void RestoreState(string json)
    {
        
    }
}