using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.ObjectChangeEventStream;

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

    private Transform unitParent;
    private Transform buildingParent;

    private Dictionary<GameObject, Queue<GameObject>> objectPools = new Dictionary<GameObject, Queue<GameObject>>();

    public static UnitManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        Register();
    }

    private void Start()
    {
        RefreshUnitList();
        RefreshStationList();
        InitializeObjectPools();
        PrewarmPools();
    }

    private void Register()
    {
        buildingPrefabs = new Dictionary<string, GameObject> {
            { "Fortress", fortressPrefab },
            { "WatchTower", watchTowerPrefab },
            { "WorkShop", workShopPrefab }
        };

        unitParent = transform.Find("Unit");
        buildingParent = transform.Find("Building");
    }

    private void Update()
    {
    }

    #region Register Methods
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
        }

        unit.OnUnitDestroyed -= OnUnitDestroyed;
        unit.OnUnitDestroyed += OnUnitDestroyed;
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
        unitObj.transform.SetParent(unitParent);
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
        unitObj.transform.SetParent(buildingParent);
        Building building = unitObj.GetComponent<Building>();

        if (building != null)
        {
            RegisterBuilding(building);
            building.name = $"{buildingType}_{allUnits.Count}";
        }

        return building;
    }
    #endregion

    #region Object Pooling
    private void InitializeObjectPools()
    {
        var spawnSettings = ObjectSpawner.Instance?.spawnSettings;
        if (spawnSettings != null)
        {
            InitializePool(archerPrefab);
            InitializePool(warriorPrefab);
            InitializePool(monkPrefab);
            InitializePool(builderPrefab);
            InitializePool(lancerPrefab);
        }
    }

    private void InitializePool(GameObject prefab)
    {
        objectPools[prefab] = new Queue<GameObject>();
    }

    public GameObject GetFromPool(GameObject prefab)
    {
        if (!objectPools.ContainsKey(prefab))
            return Instantiate(prefab);

        var pool = objectPools[prefab];
        if (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return Instantiate(prefab);
    }

    public void ReturnToPool(GameObject obj, GameObject prefab)
    {
        if (!objectPools.ContainsKey(prefab))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        objectPools[prefab].Enqueue(obj);
    }

    private void PrewarmPools()
    {
        var spawnSettings = ObjectSpawner.Instance?.spawnSettings;
        if (spawnSettings != null)
        {
            PrewarmPool(archerPrefab, 5);
            PrewarmPool(lancerPrefab, 5);
            PrewarmPool(warriorPrefab, 5);
            PrewarmPool(builderPrefab, 10);
            PrewarmPool(monkPrefab, 5);
        }
    }

    private void PrewarmPool(GameObject prefab, int count)
    {
        if (prefab != null && objectPools.ContainsKey(prefab))
        {
            for (int i = 0; i < count; i++)
            {
                var obj = Instantiate(prefab);
                var pooling = transform.Find("Object Pooling");
                if (pooling != null)
                {
                    obj.transform.SetParent(pooling);
                }
                obj.SetActive(false);
                objectPools[prefab].Enqueue(obj);
            }
        }
    }
    #endregion

    #region Management Methods
    public Unit FindUnitIdleByType(UnitType unitType)
    {
        foreach (Unit unit in allUnits)
        {
            if (unit.unitType == unitType && unit.currentState == UnitState.Idle)
            {
                Debug.Log($"Found idle {unitType} unit: {unit.unitName}");
                return unit;
            }
        }
        return null;
    }

    public Building FindUnderstaffedBuilding(UnitType unitType)
    {
        if (unitType == UnitType.Builder)
        {
            return buildings.FirstOrDefault(b => b.buildingType == BuildingType.WorkShop && b.currentCapacity < b.maxCapacity);
        }
        else
            return buildings.FirstOrDefault(b => b.buildingType != BuildingType.WorkShop && b.currentCapacity < b.maxCapacity);
    }

    #endregion

    #region Utility Methods
    public bool DeployUnitToStation(Unit unit, Building station)
    {
        if (unit == null || station == null)
            return false;

        foreach (Building currentStation in buildings)
        {
            currentStation.RemoveUnit(unit);
        }

        return station.CanAddUnit(unit);
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
                Node node = GraphNode.Instance.GetNode(new Vector3Int(cell.x, cell.y, 0), building.LayerIndex);
                if (node.isWalkable)
                    node.isWalkable = false;
            }
        }
    }
    #endregion

    
}