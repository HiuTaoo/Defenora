using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour, ISaveable
{
    public List<ISaveable> saveables = new List<ISaveable>();

    private string saveFilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    private UnitManager unitManager;

    public System.Action OnSave;

    private void Awake()
    {
        unitManager = FindObjectOfType<UnitManager>();
    }

    void Start()
    {
        saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();
        LoadGame();
    }


    public void SaveGame()
    {
        OnSave?.Invoke();

        GameSaveData saveData = new GameSaveData();

        foreach (var saveable in saveables)
        {
            saveable.PopulateSaveData(saveData);
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, json);

        Debug.Log($"Game saved to {saveFilePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found!");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        foreach (var saveable in saveables)
        {
            saveable.LoadFromSaveData(saveData);
        }

        UnitManager.Instance.UpdateGraphNodeWhenStart();

        Debug.Log($"Game loaded from {saveFilePath}");
    }

    #region Save Game
    public void PopulateSaveData(GameSaveData saveData)
    {
        var unitData = new UnitSaveData();
        foreach (var unit in unitManager.allUnits)
        {
            unitData.units.Add(new UnitData
            {
                unitName = unit.unitName,
                unitType = unit.unitType,
                position = unit.transform.position,
                assignedBuilding = unit.assignedBuilding?.buildingName,
                currentState = unit.currentState,
                health = unit.health,
                layerIndex = unit.floorAgent.currentFloorIndex,
                maxHealth = unit.maxHealth
            });
        }

        saveData.unitSaveData = unitData;

        var buildingData = new BuildingSaveData();
        foreach (var building in unitManager.buildings)
        {
            buildingData.buildings.Add(new BuildingData
            {
                buildingName = building.name,
                currentCapacity = building.currentCapacity,
                maxCapacity = building.maxCapacity,
                layerIndex = building.LayerIndex,
                archerPositions = building.listArcherPositions,
                buildingType = building.buildingType,
                position = building.transform.position,
                unitNames = building.stationedUnits
                    .Where(unit => unit != null)
                    .Select(unit => unit.unitName)
                    .ToList()
            }); ;
        }

        saveData.buildingSaveData = buildingData;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        #region Load Unit
        var unitData = saveData.unitSaveData;

        foreach (var unit in unitManager.allUnits)
            Destroy(unit.gameObject);
        unitManager.allUnits.Clear();

        foreach (var unitDatum in unitData.units)
        {
            Unit unit = unitManager.CreateUnit(unitDatum.unitType, unitDatum.position);
            unit.unitName = unitDatum.unitName;
            unit.floorAgent.MoveToFloor(unitDatum.layerIndex);
        }
        #endregion

        #region Load Building
        var buildingData = saveData.buildingSaveData;

        foreach (var building in unitManager.buildings)
            Destroy(building.gameObject);
        unitManager.buildings.Clear();

        foreach (var buildingDatum in buildingData.buildings)
        {
            Building building = unitManager.CreateBuilding(buildingDatum.buildingType, buildingDatum.position);
            building.name = buildingDatum.buildingName;
            building.LayerIndex = buildingDatum.layerIndex;
            building.UpdateRenderSortingOrder(buildingDatum.layerIndex);
        }
        #endregion
    }
    #endregion

}
