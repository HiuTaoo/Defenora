using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public UnitSaveData unitSaveData = new UnitSaveData();
    public BuildingSaveData buildingSaveData = new BuildingSaveData();
    //public InventorySaveData inventorySaveData = new InventorySaveData();
    // Thêm tuỳ module
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

// Tương tự cho các data khác...
