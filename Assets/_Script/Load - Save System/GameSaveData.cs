using System;
using System.Collections.Generic;
using _Script.Data;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int totalCoins;
    public bool isGameOver;
    public int currentDay;
    public float currentTimeInDay;
    
    public UnitSaveData unitSaveData = new UnitSaveData();
    public BuildingSaveData buildingSaveData = new BuildingSaveData();
    public ObjectSpawnData objectSpawnData = new ObjectSpawnData();
    public TaskSaveData taskSaveData = new TaskSaveData();
    public ShopSaveData shopSaveData = new ShopSaveData();
    public ItemManagerSaveData itemManagerSaveData = new ItemManagerSaveData();
    public SpawnerCycleSaveData spawnerCycleData = new();
    
    public Vector3 playerPosition;
    public int playerLayerIndex;
}

[Serializable]
public class UnitSaveData
{
    public List<UnitData> units = new List<UnitData>();
}

[Serializable]
public class BuildingSaveData
{
    public List<BuildingSaveLoadData> buildings = new List<BuildingSaveLoadData>();
}

[Serializable]
public class TaskSaveData
{
    public List<TaskData> tasks = new List<TaskData>();
}

[Serializable]
public class ObjectSpawnData
{
    public int version = 1;
    public List<LayerSpawnData> layerData = new List<LayerSpawnData>();
    public long saveTimestamp;

    public ObjectSpawnData()
    {
        saveTimestamp = DateTime.Now.ToBinary();
    }
}

