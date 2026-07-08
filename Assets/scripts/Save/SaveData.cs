// SaveData.cs
// Estruturas serializáveis que representam um ficheiro de save.
// SaveFile é o "envelope" gravado no disco; cada ISaveable guarda o seu
// próprio pedaço de JSON dentro de uma SaveEntry.

using System;
using System.Collections.Generic;

[Serializable]
public class SaveFile
{
    public string saveName;      // nome do slot (ex: "save1")
    public string dateTime;      // data/hora do save, para mostrar no menu
    public List<SaveEntry> entries = new List<SaveEntry>();
}

[Serializable]
public class SaveEntry
{
    public string id;    // corresponde ao GetUniqueId() do ISaveable
    public string json;  // estado desse objeto, em JSON
}

// --- Estruturas específicas de exemplo ---

[Serializable]
public class PlayerStateData
{
    public float posX, posY, posZ;
    public float rotY; // rotação no eixo Y (ajusta se precisares dos 3 eixos)
}

[Serializable]
public class InventoryStateData
{
    public int mineralCount;
    // Lista genérica para quando tiveres mais tipos de item no futuro.
    // Por agora fica vazia, mas já está pronta a usar.
    public List<InventoryItemData> items = new List<InventoryItemData>();
}

[Serializable]
public class InventoryItemData
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class SaveData
{
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public float playerRotY;
    public List<SaveEntry> entries = new List<SaveEntry>();
}

