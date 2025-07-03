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
        { "WatchTower", watchTowerPrefab }
    };

    }

    private void Start()
    {
        // Tìm tất cả units và building trong scene
        RefreshUnitList();
        RefreshStationList();
    }

    #region UNIT MAGAGER
    // Làm mới danh sách units
    public void RefreshUnitList()
    {
        allUnits.Clear();
        Unit[] foundUnits = FindObjectsOfType<Unit>();

        foreach (Unit unit in foundUnits)
        {
            RegisterUnit(unit);
        }
    }

    // Làm mới danh sách building
    public void RefreshStationList()
    {
        buildings.Clear();
        Building[] foundbuilding = FindObjectsOfType<Building>();
        buildings.AddRange(foundbuilding);
    }

    // Đăng ký unit mới
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

    // Xử lý khi unit bị phá hủy
    private void OnUnitDestroyed(Unit unit)
    {
        allUnits.Remove(unit);

        // Loại bỏ unit khỏi tất cả building
        foreach (Building station in buildings)
        {
            station.RemoveUnit(unit);
        }
    }

    // Tạo unit mới
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

    // Lấy prefab theo loại unit
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
            default: return null;
        }
    }

    // Điều động unit đến station
    public bool DeployUnitToStation(Unit unit, Building station)
    {
        if (unit == null || station == null)
            return false;

        // Loại bỏ unit khỏi station hiện tại (nếu có)
        foreach (Building currentStation in buildings)
        {
            currentStation.RemoveUnit(unit);
        }

        return station.AddUnit(unit);
    }

    // Thu hồi unit từ station
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

    // Lấy tất cả units theo loại
    public List<Unit> GetUnitsByType(UnitType unitType)
    {
        return allUnits.Where(u => u.unitType == unitType).ToList();
    }

    // Lấy tất cả units rảnh rỗi
    public List<Unit> GetIdleUnits()
    {
        return allUnits.Where(u => u.currentState == UnitState.Idle).ToList();
    }

    // Lấy station gần nhất
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

    // Lệnh di chuyển tất cả units đến một vị trí
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

    // Lệnh di chuyển units theo loại
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

    // Lấy thống kê tổng quan
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

    
}
