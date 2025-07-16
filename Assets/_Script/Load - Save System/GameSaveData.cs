using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public UnitSaveData unitSaveData = new UnitSaveData();
    public BuildingSaveData buildingSaveData = new BuildingSaveData();
    public ObjectSpawnData objectSpawnData = new ObjectSpawnData();

}

[System.Serializable]
public class UnitSaveData
{
    public List<UnitData> units = new List<UnitData>();
}

[System.Serializable]
public class BuildingSaveData
{
    public List<BuildingData> buildings = new List<BuildingData>();
}


[System.Serializable]
public class ObjectSpawnData
{
    public int version = 1;
    public List<LayerSpawnData> layerData = new List<LayerSpawnData>();
    public long saveTimestamp;

    public ObjectSpawnData()
    {
        saveTimestamp = System.DateTime.Now.ToBinary();
    }
}

