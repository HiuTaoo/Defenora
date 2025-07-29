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
    private TaskManager taskManager;

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
        if (taskManager == null)
            GetTaskManager();
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

        unit.OnUnitIdle -= HandlePendingTask;
        unit.OnUnitIdle += HandlePendingTask;

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
    public List<Unit> FindAllIdleBuilder()
    {
        List<Unit> idleBuilders = new List<Unit>();
        foreach (Unit unit in allUnits)
        {
            if (unit.unitType == UnitType.Builder && unit.currentState == UnitState.Idle)
            {
                idleBuilders.Add(unit);
            }
        }
        return idleBuilders;
    }

    public Queue<Builder> FindNearestBuilderQueue(Task task)
    {
        List<Unit> idleBuilders = FindAllIdleBuilder();
        List<(Builder builder, float distance)> builderDistances = new List<(Builder, float)>();

        foreach (Builder builder in idleBuilders)
        {
            var pathFinding = builder.CanMoveToTaskTarget(task);
            var distance = pathFinding?.totalCost ?? -1;

            if (distance < 0)
                continue;

            builderDistances.Add((builder, distance));
        }

        builderDistances.Sort((a, b) => a.distance.CompareTo(b.distance));

        Queue<Builder> sortedQueue = new Queue<Builder>();
        foreach (var entry in builderDistances)
        {
            sortedQueue.Enqueue(entry.builder);
        }
        Debug.Log($"Tìm thấy {sortedQueue.Count} công nhân rảnh rỗi để thực hiện công việc {task.taskType} tại {task.targetGameObject.transform.position}");
        return sortedQueue;
    }

    #region Unit Task Assignment
    public void AssignNewTaskToBuilder(Task task)
    {
        if (task == null || task.targetGameObject == null)
        {
            Debug.LogError("Task or target GameObject is null.");
            return;
        }

        if (taskManager.newTaskQueue.Count > 0)
        {
            taskManager.newTaskQueue.Dequeue();
        }

        Queue<Builder> nearestBuilders = FindNearestBuilderQueue(task);
        if (nearestBuilders.Count == 0)
        {
            Debug.LogWarning("Không có builder nào thực hiện được công việc này.");
            if (!TaskManager.Instance.pendingTask.Contains(task))
            {
                TaskManager.Instance.pendingTask.Enqueue(task);
            }
            Debug.Log($"Task {task.targetGameObject} đang được thêm vào pending tasks.");
            return;
        }

        Builder assignedBuilder = nearestBuilders.Dequeue();
        assignedBuilder.currentTask = task;
        task.listBuilders.Add(assignedBuilder);
        assignedBuilder.ExecuteTask();

        taskManager.inProgressTask.Add(task);
        Debug.Log($"Giao task {task.taskType} cho công nhân {assignedBuilder.unitName}");
    }

    public void AssignPendingTaskToBuilder(Task task, Builder builder)
    {
        if (task == null || task.targetGameObject == null || builder == null)
        {
            Debug.LogError("Task, target GameObject, or builder is null.");
            return;
        }

        if (builder.currentState != UnitState.Idle && builder.currentTask.targetGameObject != null)
        {
            Debug.LogWarning($"Builder {builder.unitName} is not idle. Cannot assign pending task.");
            if (!TaskManager.Instance.pendingTask.Contains(task))
            {
                TaskManager.Instance.pendingTask.Enqueue(task);
            }
            return;
        }

        builder.currentTask = task;
        builder.currentState = UnitState.Working;
        task.listBuilders.Add(builder);
        taskManager.inProgressTask.Add(task);
        builder.ExecuteTask();

        Debug.Log($"Assigned pending task {task.taskType} to builder {builder.unitName}");
    }
    private IEnumerator DelayAssignPendingTask(Task pendingTask, Builder builder)
    {
        yield return new WaitForSeconds(0.25f);
        AssignPendingTaskToBuilder(pendingTask, builder);
    }

    /// <summary>
    /// Kiểm tra xem có thể assign task cho builder hay không
    /// </summary>
    /// <param name="task">Task cần kiểm tra</param>
    /// <param name="builder">Builder cần assign</param>
    /// <returns>True nếu có thể assign, False nếu không thể</returns>
    private bool CanAssignTaskToBuilder(Task task, Builder builder)
    {
        if (task == null || builder == null)
            return false;

        if (task.targetGameObject == null)
            return false;

        if (builder.currentState != UnitState.Idle || builder.currentTask != null)
            return false;

        PathFinding pathFinding = builder.CanMoveToTaskTarget(task);

        if (pathFinding == null || pathFinding.totalCost < 0)
        {
            Debug.LogWarning($"Không có đường đi thích hợp cho {builder.unitName} thực hiện công việc {task.taskType} tại vị trí {task.targetGameObject.transform.position}");
            return false;
        }

        // Có thể thêm các điều kiện khác tùy vào game logic:
        // - Kiểm tra tool requirements
        // - Kiểm tra skill requirements
        // - Kiểm tra distance limits
        // - Kiểm tra resource availability

        return true;
    }

    public void CleanupTaskFromInProgress(Task task, Unit unit)
    {
        if (task == null || unit == null) return;

        if (taskManager.inProgressTask.Contains(task))
        {
            taskManager.inProgressTask.Remove(task);
            Debug.Log($"Removed task {task.taskType} from inProgressTask");
        }

        if (task.listBuilders.Contains(unit))
        {
            task.listBuilders.Remove(unit as Builder);
            Debug.Log($"Removed {unit.unitName} from task {task.taskType} builders list");
        }

        task.taskStatus = TaskStatus.NotStarted;
    }
    #endregion

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

    #region Event Handling
    private void HandleTaskCreated(Task task)
    {
        Debug.Log($"UnitManager đã nhận task mới: {task.taskType}");
        AssignNewTaskToBuilder(task);
    }

    private void HandlePendingTask(Unit unit)
    {
        if (unit.currentState != UnitState.Idle)
        {
            Debug.LogWarning($"Unit {unit.unitName} đang không rảnh. State hiện tại: {unit.currentState}");
            return;
        }

        Debug.Log($"Xử lí task trong đang tạm hoãn: {unit.unitName}, số lượng pending task: {TaskManager.Instance.pendingTask.Count}");

        if (unit.unitType != UnitType.Builder || unit.currentState != UnitState.Idle || unit.currentTask != null)
        {
            Debug.LogWarning($"Unit {unit.unitName} không phù hợp cho task. Type: {unit.unitType}, State: {unit.currentState}, HasTask: {unit.currentTask != null}");
            return;
        }

        Builder builder = unit as Builder;
        if (builder == null)
        {
            Debug.LogError($"Unit {unit.unitName} is marked as Builder but cannot cast to Builder type.");
            return;
        }

        Task assignableTask = null;

        lock (TaskManager.Instance.pendingTask)
        {
            if (TaskManager.Instance.pendingTask.Count == 0)
            {
                Debug.Log($"Không có task nào đang tạm hoãn khi unit {unit.unitName} rảnh rỗi.");
                return;
            }

            var tempQueue = new Queue<Task>();
            bool foundAssignableTask = false;

            while (TaskManager.Instance.pendingTask.Count > 0 && !foundAssignableTask)
            {
                var task = TaskManager.Instance.pendingTask.Dequeue();

                if (CanAssignTaskToBuilder(task, builder))
                {
                    assignableTask = task;
                    foundAssignableTask = true;
                    Debug.Log($"Tìm thấy task {task.taskType} được giao cho builder {builder.unitName}");
                }
                else
                {
                    tempQueue.Enqueue(task);
                    Debug.Log($"Task {task.taskType} không thể giao cho {builder.unitName}, trả lại hàng đợi");
                }
            }

            while (tempQueue.Count > 0)
            {
                TaskManager.Instance.pendingTask.Enqueue(tempQueue.Dequeue());
            }
        }

        if (assignableTask != null)
        {
            StartCoroutine(DelayAssignPendingTask(assignableTask, builder));
        }
        else
        {
            Debug.Log($"Không có pending task nào có thể giao cho {builder.unitName}");
        }
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
            priestCount = GetUnitsByType(UnitType.Monk).Count,
            warriorCount = GetUnitsByType(UnitType.Warrior).Count,
            builderCount = GetUnitsByType(UnitType.Builder).Count,
            idleUnits = GetIdleUnits().Count,
            totalBuildings = buildings.Count
        };
    }

    private void GetTaskManager()
    {
        if (TaskManager.Instance != null)
        {
            taskManager = TaskManager.Instance;
            taskManager.OnTaskCreated += HandleTaskCreated;
        }
    }
    #endregion

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
}