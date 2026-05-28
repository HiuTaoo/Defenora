using System.Collections.Generic;
using _Script.Data;
using _Script.Enum;
using UnityEngine;

[System.Serializable]
public class BuildingSaveLoadData
{
    public string buildingID;
    public string buildingName;
    public int currentCapacity;
    public int maxCapacity;
    public float currentHealth;
    public List<string> unitID;
    public List<SpotData> archerPositions;
    public int layerIndex;
    public BuildingType buildingType;
    public BuildingState buildingState;
    public Vector3 position;
    
    public List<SavedInventorySlot> storageSlots = new List<SavedInventorySlot>();
    public List<TraineeSaveData> traineeSlots = new List<TraineeSaveData>();
}

[System.Serializable]
public struct SpotData
{
    public Vector3 position;   
    public string unitName;    
}

