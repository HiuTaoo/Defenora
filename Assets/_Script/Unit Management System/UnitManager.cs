using System;
using System.Collections.Generic;
using System.Linq;
using _Script.Enum;
using _Script.Object_Pooling;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    [Header("Unit Management")]
    public List<Unit> allUnits = new();
    public List<Building> buildings = new();
    public List<Unit> enemies = new(); 
    
    public Transform buildingParent;
    public Dictionary<string, GameObject> buildingPrefabs;
    public Action OnUnitRegistered;

    private Transform unitParent;
    private Transform enemyParent; 

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
        
        enemyParent = transform.Find("Enemy");
        if (enemyParent == null)
        {
            GameObject enemyParentObj = new GameObject("Enemy");
            enemyParentObj.transform.SetParent(transform);
            enemyParent = enemyParentObj.transform;
        }
    }

    #region Register Methods

    public void RefreshUnitList()
    {
        allUnits.Clear();
        enemies.Clear(); 
        
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
        if (unit.CompareTag("Enemy"))
        {
            if (!enemies.Contains(unit))
            {
                enemies.Add(unit);
                if (enemyParent != null) unit.transform.SetParent(enemyParent);
                OnUnitRegistered?.Invoke();
            }
        }
        else
        {
            if (!allUnits.Contains(unit))
            {
                allUnits.Add(unit);
                if (unitParent != null) unit.transform.SetParent(unitParent);
                OnUnitRegistered?.Invoke();
            }
        }

        unit.OnUnitDestroyed -= OnUnitDestroyed;
        unit.OnUnitDestroyed += OnUnitDestroyed;
    }
    
    public void UnregisterUnit(Unit unit)
    {
        if (unit == null) return;

        unit.OnUnitDestroyed -= OnUnitDestroyed;

        if (allUnits.Contains(unit))
        {
            allUnits.Remove(unit);
        }

        if (enemies.Contains(unit))
        {
            enemies.Remove(unit);
        }

        foreach (var station in buildings) 
        {
            station.RemoveUnit(unit);
        }
    }

    public void RegisterBuilding(Building building)
    {
        if (!buildings.Contains(building)) buildings.Add(building);
    }

    private void OnUnitDestroyed(Unit unit)
    {
        if (unit.CompareTag("Enemy"))
        {
            if (enemies.Contains(unit)) enemies.Remove(unit);
        }
        else
        {
            if (allUnits.Contains(unit)) allUnits.Remove(unit);
            foreach (var station in buildings) station.RemoveUnit(unit);
        }
    }

    public Unit CreateUnit(UnitType unitType, Vector3 position)
    {
        var prefab = GetUnitPrefab(unitType);
        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho {unitType}");
            return null;
        }

        var unitObj = PoolManager.Instance.Spawn(prefab, position, Quaternion.identity);
        var unit = unitObj.GetComponent<Unit>();

        if (unit != null)
        {
            RegisterUnit(unit);
            
            int count = unit.CompareTag("Enemy") ? enemies.Count : allUnits.Count;
            unit.unitName = $"{unitType}_{count}";
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
            building.name = $"{buildingType}_{buildings.Count}"; 
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
        return buildings.Where(b => b.buildingType == buildingType).ToList(); 
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
        if (PrefabConfig.Instance == null)
        {
            Debug.LogError("[UnitManager] PrefabConfig.Instance chưa được khởi tạo!");
            return null;
        }

        var prefabName = unitType.ToString();
        var prefab = PrefabConfig.Instance.GetPrefab(prefabName);

        if (prefab == null)
        {
            Debug.LogError(
                $"[UnitManager] Không tìm thấy Prefab nào tên '{prefabName}' trong PrefabConfig cho loại Unit: {unitType}. Hãy đảm bảo tên GameObject của Prefab trùng với tên Enum!");
        }

        return prefab;
    }

    private GameObject GetBuildPrefab(BuildingType buildingType)
    {
        if (PrefabConfig.Instance == null)
        {
            Debug.LogError("[UnitManager] PrefabConfig.Instance chưa được khởi tạo!");
            return null;
        }

        var prefabName = buildingType.ToString();
        var prefab = PrefabConfig.Instance.GetPrefab(prefabName);

        if (prefab == null)
        {
            Debug.LogError(
                $"[UnitManager] Không tìm thấy Prefab nào tên '{prefabName}' trong PrefabConfig cho loại Building: {buildingType}. Hãy đảm bảo tên GameObject của Prefab trùng với tên Enum!");
        }

        return prefab;
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

    public GameObject GetBuildPrefabPublic(BuildingType buildingType)
    {
        return GetBuildPrefab(buildingType);
    }
    #endregion
}