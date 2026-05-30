using System;
using System.Collections.Generic;
using System.Linq;
using _Script.Enum;
using _Script.Object_Pooling;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    [Header("Unit Management")] public List<Unit> allUnits = new();

    public List<Building> buildings = new();
    private Transform buildingParent;
    public Dictionary<string, GameObject> buildingPrefabs;
    public Action OnUnitRegistered;

    private Transform unitParent;

    public static UnitManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        Register();
    }

    private void Start()
    {
        RefreshUnitList();
        RefreshStationList();
    }

    private void Register()
    {
        buildingPrefabs = new Dictionary<string, GameObject>
        {
            { "Fortress", PrefabConfig.Instance.fortressPrefab },
            { "WatchTower", PrefabConfig.Instance.watchTowerPrefab },
            { "Storage", PrefabConfig.Instance.storagePrefab },
            { "Archery", PrefabConfig.Instance.archeryPrefab },
            { "Barrack", PrefabConfig.Instance.barrackPrefab },
            { "Monastery", PrefabConfig.Instance.monasteryPrefab }
        };

        unitParent = transform.Find("Unit");
        buildingParent = transform.Find("Building");
    }

    #region Register Methods

    public void RefreshUnitList()
    {
        allUnits.Clear();
        var foundUnits = FindObjectsOfType<Unit>();

        foreach (var unit in foundUnits) RegisterUnit(unit);
    }

    public void RefreshStationList()
    {
        buildings.Clear();
        var foundbuilding = FindObjectsOfType<Building>();
        buildings.AddRange(foundbuilding);
    }

    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
            unit.transform.SetParent(unitParent);
            OnUnitRegistered?.Invoke();
        }

        unit.OnUnitDestroyed -= OnUnitDestroyed;
        unit.OnUnitDestroyed += OnUnitDestroyed;
    }


    public void RegisterBuilding(Building building)
    {
        if (!buildings.Contains(building)) buildings.Add(building);
    }

    private void OnUnitDestroyed(Unit unit)
    {
        allUnits.Remove(unit);

        foreach (var station in buildings) station.RemoveUnit(unit);
    }

    public Unit CreateUnit(UnitType unitType, Vector3 position)
    {
        var prefab = GetUnitPrefab(unitType);
        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho {unitType}");
            return null;
        }

        var unitObj = Instantiate(prefab, position, Quaternion.identity);
        unitObj.transform.SetParent(unitParent);
        var unit = unitObj.GetComponent<Unit>();

        if (unit != null)
        {
            RegisterUnit(unit);
            unit.unitName = $"{unitType}_{allUnits.Count}";
        }

        return unit;
    }

    public Building CreateBuilding(BuildingType buildingType, Vector3 position)
    {
        var prefab = GetBuildPrefab(buildingType);
        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho {buildingType}");
            return null;
        }

        var unitObj = Instantiate(prefab, position, Quaternion.identity);
        unitObj.transform.SetParent(buildingParent);
        var building = unitObj.GetComponent<Building>();

        if (building != null)
        {
            RegisterBuilding(building);
            building.name = $"{buildingType}_{allUnits.Count}";
        }

        return building;
    }

    #endregion

    #region Management Methods

    public Unit FindUnitIdleByType(UnitType unitType)
    {
        foreach (var unit in allUnits)
            if (unit.unitType == unitType && unit.currentState == UnitState.Idle)
            {
                Debug.Log($"Found idle {unitType} unit: {unit.unitName}");
                return unit;
            }

        return null;
    }

    public Building FindUnderstaffedBuilding(UnitType unitType)
    {
        if (unitType == UnitType.Builder)
            return buildings.FirstOrDefault(b =>
                b.buildingType == BuildingType.WorkShop && b.currentCapacity < b.maxCapacity);

        return buildings.FirstOrDefault(b =>
            b.buildingType != BuildingType.WorkShop && b.currentCapacity < b.maxCapacity);
    }

    public List<Building> FindBuilding(BuildingType buildingType)
    {
        var listBuilding = new List<Building>();
        foreach (var building in buildings)
            if (building.buildingType == buildingType)
                listBuilding.Add(building);
        return buildings;
    }

    public List<Building> FindBuildingNeedRepair()
    {
        return buildings.Where(b => b != null &&
                                    (b.buildingState == BuildingState.Destroyed ||
                                     (b.GetComponentInChildren<Health>() != null &&
                                      b.GetComponentInChildren<Health>().CurrentHealth <
                                      b.GetComponentInChildren<Health>().maxHealth)))
            .ToList();
    }

    public List<Unit> GetAvailableUnits()
    {
        return allUnits.Where(u => u.assignedBuilding == null
                                   && u.CompareTag("NPC")
                                   && !u.CompareTag("Enemy")
                                   && u.unitType != UnitType.Builder
                                   && u.unitType != UnitType.Civilian)
            .ToList();
    }

    #endregion

    #region Utility Methods

    public bool DeployUnitToStation(Unit unit, Building station)
    {
        if (unit == null || station == null)
            return false;

        foreach (var currentStation in buildings) currentStation.RemoveUnit(unit);

        return station.CanAddUnit(unit);
    }

    private GameObject GetUnitPrefab(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Archer: return PrefabConfig.Instance.archerPrefab;
            case UnitType.Monk: return PrefabConfig.Instance.monkPrefab;
            case UnitType.Warrior: return PrefabConfig.Instance.warriorPrefab;
            case UnitType.Builder: return PrefabConfig.Instance.builderPrefab;
            case UnitType.Lancer: return PrefabConfig.Instance.lancerPrefab;
            case UnitType.Civilian: return PrefabConfig.Instance.civilianPrefab;

            //Enemy case
            case UnitType.TorchGoblin: return PrefabConfig.Instance.torchGoblinPrefab;
            case UnitType.TNTGoblin: return PrefabConfig.Instance.tntGoblinPrefab;
            case UnitType.Barrel: return PrefabConfig.Instance.barrelPrefab;
            default: return null;
        }
    }

    private GameObject GetBuildPrefab(BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Fortress: return PrefabConfig.Instance.fortressPrefab;
            case BuildingType.WatchTower: return PrefabConfig.Instance.watchTowerPrefab;
            case BuildingType.Storage: return PrefabConfig.Instance.storagePrefab;
            case BuildingType.Archery: return PrefabConfig.Instance.archeryPrefab;
            case BuildingType.Barrack: return PrefabConfig.Instance.barrackPrefab;
            case BuildingType.Monastery: return PrefabConfig.Instance.monasteryPrefab;
            default: return null;
        }
    }

    public List<Unit> GetUnitsByType(UnitType unitType)
    {
        return allUnits.Where(u => u.unitType == unitType).ToList();
    }

    public List<Unit> GetIdleUnits()
    {
        return allUnits.Where(u => u.currentState == UnitState.Idle).ToList();
    }

    public Building GetNearestStation(Vector3 position)
    {
        Building nearest = null;
        var minDistance = float.MaxValue;

        foreach (var station in buildings)
        {
            var distance = Vector3.Distance(position, station.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = station;
            }
        }

        return nearest;
    }

    public GameObject FindBuildingPrefab(string name)
    {
        if (buildingPrefabs.TryGetValue(name, out var prefab)) return prefab;
        return null;
    }

    public GameStats GetGameStats()
    {
        return new GameStats
        {
            totalUnits = allUnits.Count,
            archerCount = GetUnitsByType(UnitType.Archer).Count,
            monkCount = GetUnitsByType(UnitType.Monk).Count,
            warriorCount = GetUnitsByType(UnitType.Warrior).Count,
            builderCount = GetUnitsByType(UnitType.Builder).Count,
            idleUnits = GetIdleUnits().Count,
            totalBuildings = buildings.Count
        };
    }

    public void UpdateGraphNodeWhenStart()
    {
        foreach (var building in buildings)
        {
            var foothPrint = building.transform.GetComponent<ObjectFootprint>();
            var cells = foothPrint.GetAbsoluteGridPositions(building.WorldToCell(building.transform.position, 1f));
            foreach (var cell in cells)
            {
                var node = GraphNode.Instance.GetNode(new Vector3Int(cell.x, cell.y, 0), building.LayerIndex);
                if (node.isWalkable)
                    node.isWalkable = false;
            }
        }
    }

    #endregion
}