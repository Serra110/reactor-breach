using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Player")]
    public Transform player;

    private CharacterController playerController;

    private string savePath;

    private Dictionary<string, ISaveable> saveables = new Dictionary<string, ISaveable>();


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        savePath = Path.Combine(Application.persistentDataPath, "save.json");

        if (player != null)
            playerController = player.GetComponent<CharacterController>();

        Debug.Log("Save Path: " + savePath);
    }



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            SaveGame();

        if (Input.GetKeyDown(KeyCode.F9))
            LoadGame();
    }



    public void SaveGame()
    {
        SaveData data = new SaveData();


        // PLAYER
        data.playerPosX = player.position.x;
        data.playerPosY = player.position.y;
        data.playerPosZ = player.position.z;
        data.playerRotY = player.eulerAngles.y;



        // INVENTÁRIO + OUTROS SISTEMAS
        foreach (var saveable in saveables.Values)
        {
            SaveEntry entry = new SaveEntry();

            entry.id = saveable.GetUniqueId();
            entry.json = saveable.CaptureState();

            data.entries.Add(entry);
        }



        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(savePath, json);


        Debug.Log("GAME SAVED!");
    }



    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Não existe save.");
            return;
        }


        string json = File.ReadAllText(savePath);

        SaveData data = JsonUtility.FromJson<SaveData>(json);



        // PLAYER
        if (playerController != null)
            playerController.enabled = false;


        player.position = new Vector3(
            data.playerPosX,
            data.playerPosY,
            data.playerPosZ
        );


        player.rotation = Quaternion.Euler(
            0,
            data.playerRotY,
            0
        );


        if (playerController != null)
            playerController.enabled = true;




        // SISTEMAS
        foreach (SaveEntry entry in data.entries)
        {
            if (saveables.TryGetValue(entry.id, out ISaveable saveable))
            {
                saveable.RestoreState(entry.json);
            }
            else
            {
                Debug.LogWarning(
                    "Não encontrei sistema: " + entry.id
                );
            }
        }


        Debug.Log("GAME LOADED!");
    }




    public void Register(ISaveable saveable)
    {
        if (saveable == null)
            return;


        string id = saveable.GetUniqueId();


        if (!saveables.ContainsKey(id))
        {
            saveables.Add(id, saveable);
            Debug.Log("Registered: " + id);
        }
    }




    public void Unregister(ISaveable saveable)
    {
        if (saveable == null)
            return;


        string id = saveable.GetUniqueId();

        if (saveables.ContainsKey(id))
            saveables.Remove(id);
    }




    public bool HasSave()
    {
        return File.Exists(savePath);
    }



    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save apagado.");
        }
    }
} 