using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Enum;
using _Script.Task;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Building : MonoBehaviour, IBuildable
{
    [Header("Configuration")] public BuildingData configData;

    [HideInInspector] public string buildingName;
    [HideInInspector] public int maxCapacity;
    [HideInInspector] public float range;
    [HideInInspector] public BuildingType buildingType;
    [HideInInspector] public int buildWoodCost;
    [HideInInspector] public int repairWoodCost;

    public int currentCapacity;
    public float currentHealth;
    public BuildingState buildingState;

    [Header("Build Progress")] [Range(0f, 100f)]
    public float currentBuildProgress;

    [Header("Unit Management")] public List<Unit> stationedUnits = new();

    [Header("Task")] public Task currentTask;

    [Tooltip("Tầng mà công trình được đặt")]
    public int layerIndex;

    [HideInInspector] public Health health;
    private Animator animator;
    private Coroutine buildEffectCoroutine;
    private CapsuleCollider2D buildingCollider;
    private ObjectFootprint buildingFootprint;

    private GameObject customRenderer;
    private bool hasBeenBuilded;

    private bool isBeingBuilded;
    public Action OnStationedUnitsChanged;

    private SpriteRenderer spriteRenderer;

    public int LayerIndex
    {
        get => layerIndex;
        set => layerIndex = value;
    }

    public virtual void Awake()
    {
        buildingFootprint = GetComponent<ObjectFootprint>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        buildingCollider = GetComponent<CapsuleCollider2D>();
        health = GetComponentInChildren<Health>();

        customRenderer = transform.Find("Custom Render Sprite")?.gameObject;

        if (configData != null)
        {
            buildingName = configData.buildingName;
            buildingType = configData.buildingType;
            maxCapacity = configData.maxCapacity;
            range = configData.range;
            buildWoodCost = configData.buildWoodCost;
            repairWoodCost = configData.repairWoodCost;
        }
        else
        {
            Debug.LogError($"[Building] {gameObject.name} thiếu Config Data!");
        }
    }

    protected virtual void Update()
    {
        if (!UnitManager.Instance.buildings.Contains(this))
            return;
        UpdateAnimation();

        if (spriteRenderer.isVisible)
        {
            if (buildingState == BuildingState.UnderConstruction && currentBuildProgress >= 100f &&
                !hasBeenBuilded) OnBuild();

            if (currentTask != null && currentTask.taskType == TaskType.RepairStructure
                                    && health.IsFull())
            {
                OnRepair();
                currentTask = null;
            }

            if ((!health.IsFull() || buildingState == BuildingState.Destroyed)
                && (currentTask == null || currentTask.targetGameObject == null))
            {
                var task = new Task(gameObject, TaskType.RepairStructure, 2, layerIndex);
                TaskManager.Instance.AddTask(task);
                currentTask = task;
            }
        }

        currentHealth = health.CurrentHealth;
    }

    /*public void SetID(string buildingID)
    {
        id = buildingID;
    }*/
    protected virtual void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnTakeDamage += HandleTakeDamage;
            health.OnDie += HandleDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnTakeDamage -= HandleTakeDamage;
            health.OnDie -= HandleDeath;
        }
    }

    public Action<IBuildable> OnBuiltObject { get; set; }

    private void UpdateAnimation()
    {
        switch (buildingState)
        {
            case BuildingState.UnderConstruction:
                animator.Play("UnderConstruction");
                ChangeTransparent(1f);
                customRenderer?.SetActive(false);
                break;
            case BuildingState.Completed:
                animator.Play("Complete");
                customRenderer?.SetActive(true);

                foreach (Transform child in transform)
                    child.gameObject.SetActive(true);

                break;
            case BuildingState.Destroyed:
                animator.Play("Destroyed");
                customRenderer?.SetActive(false);
                break;
            case BuildingState.Pending:
                animator.Play("UnderConstruction");
                ChangeTransparent(0.5f);
                break;
        }

        if (buildingState == BuildingState.Pending)
        {
            var c = spriteRenderer.color;
            c.a = 0.5f;
            spriteRenderer.color = c;
            buildingCollider.enabled = false;

            foreach (Transform child in transform) child.gameObject.SetActive(false);
        }
        else
        {
            var c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    public void CreateBuildStructureTask()
    {
        if (currentTask.targetGameObject != null)
            return;

        currentTask = new Task(
            gameObject,
            TaskType.BuildStructure,
            3,
            LayerIndex
        );

        TaskManager.Instance.AddTask(currentTask);

        Debug.Log($"[Building] Created BuildStructure task for {buildingName}");
    }

    #region Unit Management

    public virtual bool CanAddUnit(Unit unit)
    {
        if (currentCapacity >= maxCapacity)
            return false;

        if (unit.unitType == UnitType.Builder && buildingType != BuildingType.WorkShop)
            return false;

        if (unit.unitType != UnitType.Builder && buildingType == BuildingType.WorkShop)
            return false;

        if (unit.unitType == UnitType.Civilian)
            return false;

        return !stationedUnits.Contains(unit);
    }

    public virtual void AddUnit(Unit unit)
    {
        stationedUnits.Add(unit);
        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;
        currentCapacity++;

        // 🔽 Hook cho component khác
        OnUnitAdded(unit);
        OnStationedUnitsChanged?.Invoke();
    }

    public virtual bool RemoveUnit(Unit unit)
    {
        if (!stationedUnits.Contains(unit)) return false;

        stationedUnits.Remove(unit);
        unit.currentState = UnitState.Idle;
        unit.assignedBuilding = null;
        currentCapacity--;

        OnUnitRemoved(unit);
        OnStationedUnitsChanged?.Invoke();

        return true;
    }

    protected virtual void OnUnitAdded(Unit unit)
    {
        var availableSpot = GetRandomPositionAroundBuilding();
        if (availableSpot != null) unit.transform.position = availableSpot;
    }

    protected virtual void OnUnitRemoved(Unit unit)
    { }

    #endregion

    #region RANDOM POSITION

    public Vector3 GetRandomPositionAroundBuilding()
    {
        const int maxTries = 10;
        var cellSize = 1f;

        if (buildingFootprint == null)
            GetBuildingFootprinf();

        var cells = buildingFootprint.GetAbsoluteGridPositions(WorldToCell(transform.position, 1f));
        var basePosition = transform.position;
        var graphNode = GraphNode.Instance;

        var validPositions = new List<Vector3>();

        for (var i = 0; i < maxTries; i++)
        {
            var randomWorldPos = GetRandomPositionInRange(basePosition);
            var cellPos = WorldToCell(randomWorldPos, cellSize);

            var isInFootprint = false;
            foreach (var cell in cells)
                if (cellPos == cell)
                {
                    isInFootprint = true;
                    break;
                }

            if (isInFootprint) continue;

            var node = graphNode.GetNode(new Vector3Int(cellPos.x, cellPos.y, 0), LayerIndex);
            if (node != null && node.isWalkable)
            {
                var cellCenter = CellToWorld(cellPos, cellSize);
                validPositions.Add(cellCenter);
            }
        }

        if (validPositions.Count > 0)
        {
            var randomIndex = Random.Range(0, validPositions.Count);
            return validPositions[randomIndex];
        }

        Debug.LogWarning("Không tìm được Node walkable quanh Building. Trả về vị trí Building.");
        var fallbackCell = WorldToCell(basePosition, cellSize);
        return CellToWorld(fallbackCell, cellSize);
    }

    private Vector3 GetRandomPositionInRange_UniformCircle(Vector3 basePosition)
    {
        var angle = Random.Range(0f, 2f * Mathf.PI);
        var radius = Mathf.Sqrt(Random.Range(0f, 1f)) * range / 2;

        var randomOffset = new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );

        return new Vector3(
            basePosition.x + randomOffset.x,
            basePosition.y + randomOffset.y,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange_SquareToCircle(Vector3 basePosition)
    {
        Vector2 randomOffset;
        do
        {
            randomOffset = new Vector2(
                Random.Range(-range / 2, range / 2),
                Random.Range(-range / 2, range / 2)
            );
        } while (randomOffset.magnitude > range / 2);

        return new Vector3(
            basePosition.x + randomOffset.x,
            basePosition.y + randomOffset.y,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange_GridPattern(Vector3 basePosition)
    {
        var gridSize = Mathf.RoundToInt(range);
        var randomX = Random.Range(-gridSize / 2, gridSize / 2 + 1);
        var randomY = Random.Range(-gridSize / 2, gridSize / 2 + 1);

        var noiseX = Random.Range(-0.3f, 0.3f);
        var noiseY = Random.Range(-0.3f, 0.3f);

        return new Vector3(
            basePosition.x + randomX + noiseX,
            basePosition.y + randomY + noiseY,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange_WeightedDistance(Vector3 basePosition)
    {
        var minDistance = 2f;
        var maxDistance = range / 2;

        var angle = Random.Range(0f, 2f * Mathf.PI);
        var radius = Random.Range(minDistance, maxDistance);

        var randomOffset = new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );

        return new Vector3(
            basePosition.x + randomOffset.x,
            basePosition.y + randomOffset.y,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange(Vector3 basePosition)
    {
        var method = Random.Range(0, 4);

        switch (method)
        {
            case 0: return GetRandomPositionInRange_UniformCircle(basePosition);
            case 1: return GetRandomPositionInRange_SquareToCircle(basePosition);
            case 2: return GetRandomPositionInRange_GridPattern(basePosition);
            case 3: return GetRandomPositionInRange_WeightedDistance(basePosition);
            default: return GetRandomPositionInRange_UniformCircle(basePosition);
        }
    }

    #endregion

    #region Helper Methods

    public Vector2Int WorldToCell(Vector3 worldPos, float cellSize)
    {
        var x = Mathf.FloorToInt(worldPos.x / cellSize);
        var y = Mathf.FloorToInt(worldPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorld(Vector2Int cellPos, float cellSize)
    {
        return new Vector3(
            (cellPos.x + 0.5f) * cellSize,
            (cellPos.y + 0.5f) * cellSize,
            0f
        );
    }

    public void UpdateRenderSortingOrder(int layerIndex)
    {
        GetSpriteRenderer();
    }

    private void GetSpriteRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void GetBuildingFootprinf()
    {
        if (buildingFootprint == null)
            buildingFootprint = GetComponent<ObjectFootprint>();
    }

    public BuildingSaveLoadData GetStationInfo()
    {
        return new BuildingSaveLoadData
        {
            buildingName = buildingName,
            currentCapacity = stationedUnits.Count,
            maxCapacity = maxCapacity,
            unitID = stationedUnits.Select(u => u.unitName).ToList()
        };
    }

    public SpriteRenderer GetSpriteRendererComponent()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        return spriteRenderer;
    }

    private void ChangeTransparent(float cap)
    {
        var c = spriteRenderer.color;
        c.a = cap;
        spriteRenderer.color = c;
    }

    public virtual void ForceAddUnitOnLoad(Unit unit)
    {
        if (!stationedUnits.Contains(unit)) stationedUnits.Add(unit);

        unit.assignedBuilding = this;
    }

    #endregion

    #region Build

    public void HandleBuilt(float buildSpeed)
    {
        if (currentBuildProgress >= 100f) return;

        currentBuildProgress = currentBuildProgress + buildSpeed;

        if (!isBeingBuilded && buildEffectCoroutine == null) buildEffectCoroutine = StartCoroutine(BuildEffect());
    }

    public void HandleRepair()
    {
        if (health.IsFull() && buildingState == BuildingState.Completed) return;

        health.RepairBuilding(health.maxHealth / 20);

        if (!isBeingBuilded && buildEffectCoroutine == null) buildEffectCoroutine = StartCoroutine(BuildEffect());
    }

    public void OnBuild()
    {
        hasBeenBuilded = true;
        buildingState = BuildingState.Completed;

        if (currentTask != null)
        {
            currentTask.Complete();
            currentTask = null;
        }

        if (buildingType == BuildingType.Storage) Inventory.Instance.RefreshStorageSubscriptions();

        health.RestoreHealth();

        OnBuiltObject?.Invoke(this);
    }

    public void OnRepair()
    {
        hasBeenBuilded = true;
        buildingState = BuildingState.Completed;

        if (currentTask != null)
        {
            currentTask.Complete();
            currentTask = null;
        }

        OnBuiltObject?.Invoke(this);
    }


    private IEnumerator BuildEffect()
    {
        isBeingBuilded = true;

        spriteRenderer.color = new Color32(207, 207, 207, 255);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;

        isBeingBuilded = false;
        buildEffectCoroutine = null;
    }

    public bool HasObstacleAroundBuilding(float radius = 3f)
    {
        var center = transform.position;

        var hits = Physics2D.OverlapCircleAll(center, radius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            if (hit.TryGetComponent<IChoppable>(out var choppable)) return true;
        }

        return false;
    }

    public IChoppable FindObstacleObject(float radius = 2f)
    {
        var center = transform.position;

        var hits = Physics2D.OverlapCircleAll(center, radius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject && !hit.gameObject.activeInHierarchy)
                continue;

            if (hit.TryGetComponent<IChoppable>(out var choppable) && !choppable.IsClaimed)
            {
                if (choppable is Tree)
                    continue;
                return choppable;
            }
        }

        return null;
    }

    #endregion

    #region Action

    protected virtual void HandleHealthChanged(float current, float max)
    {
    }

    protected virtual void HandleTakeDamage(float damage)
    {
        if (gameObject.activeInHierarchy && !isBeingBuilded) StartCoroutine(DamageEffect());
    }

    protected virtual void HandleDeath()
    {
        buildingState = BuildingState.Destroyed;

        EvacuateAllUnits();
    }

    private IEnumerator DamageEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (buildingState == BuildingState.Pending)
            ChangeTransparent(0.5f);
        else
            spriteRenderer.color = Color.white;
    }

    private void EvacuateAllUnits()
    {
        for (var i = stationedUnits.Count - 1; i >= 0; i--)
        {
            var unit = stationedUnits[i];
            RemoveUnit(unit);
        }
    }

    #endregion
}