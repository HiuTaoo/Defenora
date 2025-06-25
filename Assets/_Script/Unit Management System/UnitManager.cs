using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    [Header("Unit Management")]
    public List<Unit> allUnits = new List<Unit>();
    public List<Station> stations = new List<Station>();

    [Header("Unit Prefabs")]
    public GameObject archerPrefab;
    public GameObject priestPrefab;
    public GameObject warriorPrefab;
    public GameObject builderPrefab;

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
    }

    private void Start()
    {
        // Tìm tất cả units và stations trong scene
        RefreshUnitList();
        RefreshStationList();
    }

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

    // Làm mới danh sách stations
    public void RefreshStationList()
    {
        stations.Clear();
        Station[] foundStations = FindObjectsOfType<Station>();
        stations.AddRange(foundStations);
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

    // Xử lý khi unit bị phá hủy
    private void OnUnitDestroyed(Unit unit)
    {
        allUnits.Remove(unit);

        // Loại bỏ unit khỏi tất cả stations
        foreach (Station station in stations)
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

    // Lấy prefab theo loại unit
    private GameObject GetUnitPrefab(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Archer: return archerPrefab;
            case UnitType.Priest: return priestPrefab;
            case UnitType.Warrior: return warriorPrefab;
            case UnitType.Builder: return builderPrefab;
            default: return null;
        }
    }

    // Điều động unit đến station
    public bool DeployUnitToStation(Unit unit, Station station)
    {
        if (unit == null || station == null)
            return false;

        // Loại bỏ unit khỏi station hiện tại (nếu có)
        foreach (Station currentStation in stations)
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

        foreach (Station station in stations)
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
    public Station GetNearestStation(Vector3 position)
    {
        Station nearest = null;
        float minDistance = float.MaxValue;

        foreach (Station station in stations)
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

    // Lấy thống kê tổng quan
    public GameStats GetGameStats()
    {
        return new GameStats
        {
            totalUnits = allUnits.Count,
            archerCount = GetUnitsByType(UnitType.Archer).Count,
            priestCount = GetUnitsByType(UnitType.Priest).Count,
            warriorCount = GetUnitsByType(UnitType.Warrior).Count,
            builderCount = GetUnitsByType(UnitType.Builder).Count,
            idleUnits = GetIdleUnits().Count,
            totalStations = stations.Count
        };
    }
}
