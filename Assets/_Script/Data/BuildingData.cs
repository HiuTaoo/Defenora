using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

[System.Serializable]
public struct BuildingData
{
    public string buildingName;
    public int currentCapacity;
    public int maxCapacity;
    public float maxHealth;
    public float currentHealth;
    public List<string> unitNames;
    public List<SpotData> archerPositions;
    public int layerIndex;
    public BuildingType buildingType;
    public BuildingState buildingState;
    public Vector3 position;
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
    WorkShop
}

public enum BuildingState
{
    Placing,
    Pending,
    UnderConstruction,
    Completed,
    Destroyed
}