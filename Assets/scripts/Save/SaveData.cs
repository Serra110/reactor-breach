

using System;
using System.Collections.Generic;

[Serializable]
public class SaveFile
{
    public string saveName;      
    public string dateTime;     
    public List<SaveEntry> entries = new List<SaveEntry>();
}

[Serializable]
public class SaveEntry
{
    public string id;    
    public string json;  
}



[Serializable]
public class PlayerStateData
{
    public float posX, posY, posZ;
    public float rotY; 
}

[Serializable]
public class InventoryStateData
{
    public int mineralCount;
    public List<InventoryItemData> items = new List<InventoryItemData>();
}

[Serializable]
public class InventoryItemData
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class InventorySlotItemSave
{
    public string itemName;
    public int amount;
}

[Serializable]
public class InventorySlotSaveData
{
    public int panelSize;
    public int hotbarSize;
    public int selectedHotbar;
    public List<InventorySlotItemSave> panel = new List<InventorySlotItemSave>();
    public List<InventorySlotItemSave> hotbar = new List<InventorySlotItemSave>();
}



[Serializable]
public class MachineSpawnData
{
    public string id;
    public string machineType;
    public float posX, posY, posZ;
    public float rotY;
}

[Serializable]
public class SaveData
{
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public float playerRotY;
    public List<SaveEntry> entries = new List<SaveEntry>();

    // NOVO
    public List<MachineSpawnData> machines = new List<MachineSpawnData>();
}