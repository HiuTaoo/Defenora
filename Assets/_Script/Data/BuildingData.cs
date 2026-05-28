using System.Collections.Generic;
using _Script.Data;
using UnityEngine;

[System.Serializable]
public class BuildingData
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
}

[System.Serializable]
public struct SpotData
{
    public Vector3 position;   
    public string unitName;    
}

public enum BuildingType
{
    Fortress,
    WatchTower,
    WorkShop,
    Storage,
    Archery,
    Barrack,
    Monastery
}

public enum BuildingState
{
    Placing,
    Pending,
    UnderConstruction,
    Completed,
    Destroyed
}