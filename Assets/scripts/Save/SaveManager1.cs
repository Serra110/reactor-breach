using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Player")]
    public Transform player;

    // NOVO
    [Header("Prefabs de máquinas (para recriar no load)")]
    public List<GameObject> machinePrefabs;
    private Dictionary<string, GameObject> prefabLookup;

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

        prefabLookup = new Dictionary<string, GameObject>();
        foreach (var prefab in machinePrefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[SaveManager] machinePrefab nulo na lista.");
                continue;
            }

            var placeable = prefab.GetComponent<IPlaceable>();
            if (placeable != null)
                prefabLookup[placeable.GetMachineType()] = prefab;
        }
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
        if (player != null)
        {
            data.playerPosX = player.position.x;
            data.playerPosY = player.position.y;
            data.playerPosZ = player.position.z;
            data.playerRotY = player.eulerAngles.y;
        }



        // INVENTÁRIO + OUTROS SISTEMAS + MÁQUINAS
        foreach (var saveable in saveables.Values)
        {
            SaveEntry entry = new SaveEntry();

            entry.id = saveable.GetUniqueId();
            entry.json = saveable.CaptureState();

            data.entries.Add(entry);

            // NOVO: se for uma máquina colocável, guarda também posição/rotação/tipo
            if (saveable is IPlaceable placeable)
            {
                var mono = (MonoBehaviour)saveable;

                data.machines.Add(new MachineSpawnData
                {
                    id = saveable.GetUniqueId(),
                    machineType = placeable.GetMachineType(),
                    posX = mono.transform.position.x,
                    posY = mono.transform.position.y,
                    posZ = mono.transform.position.z,
                    rotY = mono.transform.eulerAngles.y
                });
            }
        }



        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(savePath, json);
    }



    public void LoadGame()
    {
        if (!File.Exists(savePath))
            return;


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



        // NOVO: (a) destruir todas as máquinas atuais da cena
        List<string> idsParaRemover = new List<string>();
        foreach (var kvp in saveables)
        {
            if (kvp.Value is IPlaceable)
            {
                idsParaRemover.Add(kvp.Key);
                Destroy(((MonoBehaviour)kvp.Value).gameObject);
            }
        }
        foreach (var id in idsParaRemover)
            saveables.Remove(id);


        // NOVO: (b) recriar as máquinas a partir do save
        foreach (var spawnData in data.machines)
        {
            if (!prefabLookup.TryGetValue(spawnData.machineType, out GameObject prefab))
            {
                Debug.LogWarning("Prefab não encontrado: " + spawnData.machineType);
                continue;
            }

            Vector3 pos = new Vector3(spawnData.posX, spawnData.posY, spawnData.posZ);
            Quaternion rot = Quaternion.Euler(0, spawnData.rotY, 0);

            GameObject instance = Instantiate(prefab, pos, rot);
            var handler = instance.GetComponent<MachineSaveHandler>();

            if (handler != null)
                handler.AssignSavedId(spawnData.id);
        }



        // SISTEMAS (agora já inclui as máquinas recém-criadas, registadas via AssignSavedId)
        foreach (SaveEntry entry in data.entries)
        {
            if (saveables.TryGetValue(entry.id, out ISaveable saveable))
            {
                saveable.RestoreState(entry.json);
            }
        }
    }




    public void Register(ISaveable saveable)
    {
        if (saveable == null)
            return;


        string id = saveable.GetUniqueId();


        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[SaveManager] Registado com ID vazio: " + saveable.GetType().Name);
            return;
        }

        if (!saveables.ContainsKey(id))
        {
            saveables.Add(id, saveable);
        }
        else if (saveables[id] != saveable)
        {
            Debug.LogWarning("[SaveManager] Colisão de ID de save: \"" + id + "\" já registado em " +
                             saveables[id].GetType().Name + " — " + saveable.GetType().Name + " NÃO foi registado.");
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
            File.Delete(savePath);
    }
}