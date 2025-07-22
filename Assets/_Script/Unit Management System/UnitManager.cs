using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitManager : MonoBehaviour
{
    [Header("Unit Management")]
    public List<Unit> allUnits = new List<Unit>();
    public List<Building> buildings = new List<Building>();
    public Dictionary<string, GameObject> buildingPrefabs;

    [Header("Unit Prefabs")]
    public GameObject archerPrefab;
    public GameObject monkPrefab;
    public GameObject warriorPrefab;
    public GameObject builderPrefab;
    public GameObject lancerPrefab;

    [Header("Building Prefab")]
    public GameObject fortressPrefab;
    public GameObject watchTowerPrefab;
    public GameObject workShopPrefab;

    // Singleton pattern
    public static UnitManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        buildingPrefabs = new Dictionary<string, GameObject> {
        { "Fortress", fortressPrefab },
        { "WatchTower", watchTowerPrefab },
        { "WorkShop", workShopPrefab }
    };

    }

    private void Start()
    {
        RefreshUnitList();
        RefreshStationList();
    }

    #region UNIT MAGAGER
    public void RefreshUnitList()
    {
        allUnits.Clear();
        Unit[] foundUnits = FindObjectsOfType<Unit>();

        foreach (Unit unit in foundUnits)
        {
            RegisterUnit(unit);
        }
    }

    public void RefreshStationList()
    {
        buildings.Clear();
        Building[] foundbuilding = FindObjectsOfType<Building>();
        buildings.AddRange(foundbuilding);
    }

    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
            unit.OnUnitDestroyed += OnUnitDestroyed;
        }
    }

    public void RegisterBuilding(Building building)
    {
        if (!buildings.Contains(building))
        {
            buildings.Add(building);

        }
    }

    private void OnUnitDestroyed(Unit unit)
    {
        allUnits.Remove(unit);

        foreach (Building station in buildings)
        {
            station.RemoveUnit(unit);
        }
    }

    public Unit CreateUnit(UnitType unitType, Vector3 position)
    {
        GameObject prefab = GetUnitPrefab(unitType);
        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho {unitType}");
            return null;
        }

        GameObject unitObj = Instantiate(prefab, position, Quaternion.identity);
        Unit unit = unitObj.GetComponent<Unit>();

        if (unit != null)
        {
            RegisterUnit(unit);
            unit.unitName = $"{unitType}_{allUnits.Count}";
        }

        return unit;
    }

    public Building CreateBuilding(BuildingType buildingType, Vector3 position)
    {
        GameObject prefab = GetBuildPrefab(buildingType);
        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho {buildingType}");
            return null;
        }

        GameObject unitObj = Instantiate(prefab, position, Quaternion.identity);
        Building building = unitObj.GetComponent<Building>();

        if (building != null)
        {
            RegisterBuilding(building);
            building.name = $"{buildingType}_{allUnits.Count}";
        }

        return building;
    }

    private GameObject GetUnitPrefab(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Archer: return archerPrefab;
            case UnitType.Monk: return monkPrefab;
            case UnitType.Warrior: return warriorPrefab;
            case UnitType.Builder: return builderPrefab;
            case UnitType.Lancer: return lancerPrefab;
            default: return null;
        }
    }

    private GameObject GetBuildPrefab(BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Fortress: return fortressPrefab;
            case BuildingType.WatchTower: return watchTowerPrefab;
            case BuildingType.WorkShop: return workShopPrefab;
            default: return null;
        }
    }

    public bool DeployUnitToStation(Unit unit, Building station)
    {
        if (unit == null || station == null)
            return false;

        foreach (Building currentStation in buildings)
        {
            currentStation.RemoveUnit(unit);
        }

        return station.AddUnit(unit);
    }

    public bool RecallUnit(Unit unit)
    {
        if (unit == null)
            return false;

        foreach (Building station in buildings)
        {
            if (station.RemoveUnit(unit))
            {
                unit.StopMovement();
                return true;
            }
        }

        return false;
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
        float minDistance = float.MaxValue;

        foreach (Building station in buildings)
        {
            float distance = Vector3.Distance(position, station.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = station;
            }
        }

        return nearest;
    }

    public void MoveAllUnitsTo(Vector3 destination)
    {
        foreach (Unit unit in allUnits)
        {
            if (unit.currentState == UnitState.Idle || unit.currentState == UnitState.Moving)
            {
                unit.MoveTo(destination);
            }
        }
    }

    public void MoveUnitsByTypeTo(UnitType unitType, Vector3 destination)
    {
        List<Unit> targetUnits = GetUnitsByType(unitType);

        foreach (Unit unit in targetUnits)
        {
            if (unit.currentState == UnitState.Idle || unit.currentState == UnitState.Moving)
            {
                unit.MoveTo(destination);
            }
        }
    }

    public GameObject FindBuildingPrefab(string name)
    {
        if (buildingPrefabs.TryGetValue(name, out var prefab))
        {
            return prefab;
        }
        return null;
    }

    public GameStats GetGameStats()
    {
        return new GameStats
        {
            totalUnits = allUnits.Count,
            archerCount = GetUnitsByType(UnitType.Archer).Count,
            priestCount = GetUnitsByType(UnitType.Monk).Count,
            warriorCount = GetUnitsByType(UnitType.Warrior).Count,
            builderCount = GetUnitsByType(UnitType.Builder).Count,
            idleUnits = GetIdleUnits().Count,
            totalBuildings = buildings.Count
        };
    }
#endregion

    public void UpdateGraphNodeWhenStart()
    {
        foreach (var building in buildings)
        {
            var foothPrint = building.transform.GetComponent<BuildingFootprint>();
            var cells = foothPrint.GetAbsoluteGridPositions(building.WorldToCell(building.transform.position, 1f));
            foreach (var cell in cells)
            {
                Node node = GraphNode.Instance.GetNode(new Vector3Int(cell.x, cell.y, 0), building.LayerIndex);
                if (node.isWalkable)
                    node.isWalkable = false;
            }
        }
    }
    
}
