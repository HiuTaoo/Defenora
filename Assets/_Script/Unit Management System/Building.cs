using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Task;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public abstract class Building : MonoBehaviour, IBuildable
{
    [Header("Building Info")]
    public string buildingName;
    public int maxCapacity = 5;
    public float range = 5f;
    public int currentCapacity = 0;
    public float currentHealth;
    public BuildingType buildingType;
    public BuildingState buildingState;

    [Header("Build Progress")]
    [Range(0f, 100f)]
    public float currentBuildProgress = 0f; 
    public float buildSpeedPerHit = 10f; 

    [Header("Unit Management")]
    public List<Unit> stationedUnits = new List<Unit>();

    [Header("Task")]
    public Task currentTask;

    [Tooltip("Tầng mà công trình được đặt")]
    public int layerIndex = 0;

    private SpriteRenderer spriteRenderer;
    private ObjectFootprint buildingFootprint;
    private Animator animator;
    private CapsuleCollider2D buildingCollider;

    private GameObject customRenderer;
    private Coroutine buildEffectCoroutine;
    public Action<IBuildable> OnBuiltObject { get; set; }
    public Health health;

    private bool isBeingBuilded = false;
    private bool hasBeenBuilded = false;

    public int LayerIndex
    {
        get => layerIndex;
        set
        {
            layerIndex = value;
        }
    }

    public virtual void Awake()
    {
        buildingFootprint = GetComponent<ObjectFootprint>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        buildingCollider = GetComponent<CapsuleCollider2D>();
        health = GetComponentInChildren<Health>();

        customRenderer = transform.Find("Custom Render Sprite")?.gameObject;

        buildingName = gameObject.name;
    }

    private void Update()
    {
        UpdateAnimation();

        if (spriteRenderer.isVisible)
        {
            if (buildingState == BuildingState.UnderConstruction && currentBuildProgress >= 100f && !hasBeenBuilded)
            {
                OnBuild();
            }

            if (currentTask != null && currentTask.taskType == TaskType.RepairStructure
                && health.IsFull())
            {
                OnRepair();
                currentTask = null;
            }
        }

        currentHealth = health.CurrentHealth;
    }

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
            Color c = spriteRenderer.color;
            c.a = 0.5f;
            spriteRenderer.color = c;
            buildingCollider.enabled = false;

            foreach(Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    public void CreateBuildStructureTask()
    {
        if (currentTask.targetGameObject != null)
            return;

        currentTask = new Task(
            target: this.gameObject,
            type: TaskType.BuildStructure,
            maxBuilders: 3,
            layerIndex: LayerIndex
        );

        TaskManager.Instance.AddTask(currentTask);

        Debug.Log($"[Building] Created BuildStructure task for {buildingName}");
    }


    /*#region Unit Management
    public bool CanAddUnit(Unit unit)
    {
        if (currentCapacity >= maxCapacity)
        {
            Debug.Log($"Trạm {buildingName} đã đầy!");
            return false;
        }
        if (unit.unitType == UnitType.Builder && buildingType != BuildingType.WorkShop)
            return false;

        if (unit.unitType != UnitType.Builder && buildingType == BuildingType.WorkShop)
            return false;

        return !stationedUnits.Contains(unit);
    }

    public virtual void AddUnit(Unit unit)
    {
        stationedUnits.Add(unit);
        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;
        currentCapacity++;

        Debug.Log($"Register {unit.name} to Building: {this.name}");

        if (GameLoop.Instance.StateMachine.CurrentState is EditorState)
            RegisterUnitPosition(unit);

        unit.currentState = UnitState.Stationed;
        //unit.floorAgent.MoveToFloor(LayerIndex);
    }

    private void RegisterUnitPosition(Unit unit)
    {
        if (unit.unitType == UnitType.Archer)
        {
            Vector3 spot = GetAvailableSpot();
            if (spot != null)
            {
                unit.transform.position = spot;
                unit.spriteRenderer.sortingOrder = spriteRenderer.sortingOrder + 10;

                listArcherPositions.Add(new SpotData
                {
                    position = spot,
                    unitName = unit.gameObject.name
                });
            }
        }
        else
        {
            Vector3 availableSpot = GetRandomPositionAroundBuilding();
            if (availableSpot != null)
            {
                unit.transform.position = availableSpot;
            }
        }
    }

    public virtual bool RemoveUnit(Unit unit)
    {
        if (stationedUnits.Contains(unit))
        {
            stationedUnits.Remove(unit);
            unit.currentState = UnitState.Idle;
            unit.assignedBuilding = null;
            currentCapacity--;

            if (unit.unitType == UnitType.Archer)
            {
                int index = listArcherPositions.FindIndex(s => s.unitName == unit.unitName);
                if (index >= 0)
                {
                    var removed = listArcherPositions[index];
                    listArcherPositions.RemoveAt(index);
                }
            }
            return true;
        }
        return false;
    }

    
    #endregion*/
    
    #region Unit Management
    public virtual bool CanAddUnit(Unit unit)
    {
        if (currentCapacity >= maxCapacity)
            return false;

        if (unit.unitType == UnitType.Builder && buildingType != BuildingType.WorkShop)
            return false;

        if (unit.unitType != UnitType.Builder && buildingType == BuildingType.WorkShop)
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
    }

    public virtual bool RemoveUnit(Unit unit)
    {
        if (!stationedUnits.Contains(unit)) return false;

        stationedUnits.Remove(unit);
        unit.currentState = UnitState.Idle;
        unit.assignedBuilding = null;
        currentCapacity--;

        OnUnitRemoved(unit);

        return true;
    }

// 🔑 HOOK — mặc định không làm gì
    protected virtual void OnUnitAdded(Unit unit) { }
    protected virtual void OnUnitRemoved(Unit unit) { }

    #endregion

    #region RANDOM POSITION
    public Vector3 GetRandomPositionAroundBuilding()
    {
        const int maxTries = 10; 
        float cellSize = 1f;

        if (buildingFootprint == null)
            GetBuildingFootprinf();

        var cells = buildingFootprint.GetAbsoluteGridPositions(WorldToCell(transform.position, 1f));
        Vector3 basePosition = transform.position;
        GraphNode graphNode = GraphNode.Instance;

        List<Vector3> validPositions = new List<Vector3>();

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomWorldPos = GetRandomPositionInRange(basePosition);
            Vector2Int cellPos = WorldToCell(randomWorldPos, cellSize);

            bool isInFootprint = false;
            foreach (var cell in cells)
            {
                if (cellPos == cell)
                {
                    isInFootprint = true;
                    break;
                }
            }

            if (isInFootprint) continue;

            Node node = graphNode.GetNode(new Vector3Int(cellPos.x, cellPos.y, 0), LayerIndex);
            if (node != null && node.isWalkable)
            {
                Vector3 cellCenter = CellToWorld(cellPos, cellSize);
                validPositions.Add(cellCenter);
            }
        }

        if (validPositions.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validPositions.Count);
            return validPositions[randomIndex];
        }

        Debug.LogWarning($"Không tìm được Node walkable quanh Building. Trả về vị trí Building.");
        Vector2Int fallbackCell = WorldToCell(basePosition, cellSize);
        return CellToWorld(fallbackCell, cellSize);
    }

    private Vector3 GetRandomPositionInRange_UniformCircle(Vector3 basePosition)
    {
        float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        float radius = Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f)) * range / 2;

        Vector2 randomOffset = new Vector2(
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
                UnityEngine.Random.Range(-range / 2, range / 2),
                UnityEngine.Random.Range(-range / 2, range / 2)
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
        int gridSize = Mathf.RoundToInt(range);
        int randomX = UnityEngine.Random.Range(-gridSize / 2, gridSize / 2 + 1);
        int randomY = UnityEngine.Random.Range(-gridSize / 2, gridSize / 2 + 1);

        float noiseX = UnityEngine.Random.Range(-0.3f, 0.3f);
        float noiseY = UnityEngine.Random.Range(-0.3f, 0.3f);

        return new Vector3(
            basePosition.x + randomX + noiseX,
            basePosition.y + randomY + noiseY,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange_WeightedDistance(Vector3 basePosition)
    {
        float minDistance = 2f; 
        float maxDistance = range / 2;

        float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        float radius = UnityEngine.Random.Range(minDistance, maxDistance);

        Vector2 randomOffset = new Vector2(
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
        int method = UnityEngine.Random.Range(0, 4);

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
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.y / cellSize);
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
        if(buildingFootprint == null)
            buildingFootprint = GetComponent<ObjectFootprint>();
    }

    public BuildingData GetStationInfo()
    {
        return new BuildingData
        {
            buildingName = this.buildingName,
            currentCapacity = stationedUnits.Count,
            maxCapacity = this.maxCapacity,
            unitNames = stationedUnits.Select(u => u.unitName).ToList()
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
        Color c = spriteRenderer.color;
        c.a = cap;
        spriteRenderer.color = c;
    }
    #endregion

    #region Build
    public void HandleBuilt()
    {
        if (currentBuildProgress >= 100f) return;

        currentBuildProgress = currentBuildProgress + buildSpeedPerHit;

        if (!isBeingBuilded && buildEffectCoroutine == null)
        {
            buildEffectCoroutine =  StartCoroutine(BuildEffect());
        }
    }
    
    public void HandleRepair()
    {
        if (health.IsFull() && buildingState == BuildingState.Completed) return;

        health.RepairBuilding(health.maxHealth / 20);

        if (!isBeingBuilded && buildEffectCoroutine == null)
        {
            buildEffectCoroutine =  StartCoroutine(BuildEffect());
        }
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
    
    public bool HasObstacleAroundBuilding( float radius = 3f)
    {
        Vector3 center = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;
            
            if (hit.TryGetComponent<IChoppable>(out var choppable))
            {
                return true; 
            }
        }
        return false;
    }
    
    public IChoppable FindObstacleObject( float radius = 2f)
    {
        Vector3 center = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject && !hit.gameObject.activeInHierarchy)
                continue;
            
            if (hit.TryGetComponent<IChoppable>(out var choppable) && !choppable.IsClaimed)
            {
                if(choppable is Tree)
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
        if (gameObject.activeInHierarchy && !isBeingBuilded) 
        {
            StartCoroutine(DamageEffect());
        }
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
        for (int i = stationedUnits.Count - 1; i >= 0; i--)
        {
            Unit unit = stationedUnits[i];
            RemoveUnit(unit);
        }
    }

    #endregion
    
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

}
