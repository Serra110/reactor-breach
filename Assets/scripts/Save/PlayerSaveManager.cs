using UnityEngine;

public class PlayerSaveHandler : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueId = "Player";

    private void OnEnable()
    {
        SaveManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.Unregister(this);
    }

    public string GetUniqueId() => uniqueId;

    public string CaptureState()
    {
        var data = new PlayerStateData
        {
            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z,
            rotY = transform.eulerAngles.y
        };

        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        var data = JsonUtility.FromJson<PlayerStateData>(json);

        transform.position = new Vector3(data.posX, data.posY, data.posZ);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, data.rotY, transform.eulerAngles.z);

    }
}
