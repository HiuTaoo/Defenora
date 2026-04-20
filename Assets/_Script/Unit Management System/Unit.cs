using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.BT;
using _Script.BT.GlobalAlarm;
using _Script.Enum;
using _Script.Unit_Management_System.Animation;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    public string unitName;
    public UnitType unitType;
    public UnitState currentState = UnitState.Idle;
    public AnimState animState = AnimState.Idle;
    public int layerIndex;
    public float currentHealth;
    
    [Header("Deployment")]
    public Building assignedBuilding;  
    
    [Header("Layer")]
    public int obstacleLayer;
    public int enemyLayer;
    
    [Header("Array Non Alloc")]
    public Collider2D[] results;
    
    [Header("Target")]
    public Transform currentTarget;
    public int currentTargetLayerIndex;
    public Vector2 lastSeenPosition;
    public int lastSeenLayerIndex;
    public bool isAlerted;
    public float aggroTimer;
    public float aggroDuration = 5f;
    
    [Header("Sensor")]
    public float detectTimer = 0f;
    public float detectInterval = 0.25f;
    public float hearRange = 15f;
    
    [Header("Animation FSM")]
    public AnimationFSM animFSM;
    
    [Header("Stats System")]
    public UnitStatsManager statsManager;

    public GameObject enemySpawnPoint;

    protected BehaviourTree bt;
    private Rigidbody2D rb;
    public DynamicSortingYX sortingYX;
    public Health health;
    
    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public CharacterMovement characterMovement;
    [HideInInspector] public FloorAgent floorAgent;

    [Header("Combat Stats")] 
    public float attackRange = 0f;
    public float attackCooldown = 1f; 
    public float lastAttackTime = -999f;
    public bool isKnockedBack = false;
    public float hitStunDuration = 0.1f;
    public float knockbackCooldown = 3f; 
    private float lastKnockbackTime = -999f;
    
    [Header("Vision")]
    [Range(0, 360)]
    public float viewAngle;
    
    public float attackDamage => statsManager != null ? statsManager.AttackDamage : 0;
    public float viewDistance => statsManager != null ? statsManager.ViewDistance : 0;
    
    [Header("Effects")]
    private Coroutine damageEffectCoroutine; 
    private Coroutine hitStunCoroutine;
    public bool isAttacking { get; protected set; }
    public bool isInWindup { get; protected set; }

    public System.Action<Unit> OnUnitDestroyed;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterMovement = GetComponentInChildren<CharacterMovement>();
        floorAgent = GetComponentInChildren<FloorAgent>();
        animFSM = GetComponent<AnimationFSM>();
        sortingYX = GetComponent<DynamicSortingYX>();
        health = GetComponentInChildren<Health>();
        statsManager = GetComponentInChildren<UnitStatsManager>();
                        
        enemyLayer = LayerMask.GetMask("NPC");
        obstacleLayer = LayerMask.GetMask("VisionBlocker");
        results = new Collider2D[20];

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        unitName = gameObject.name;
        UpdateHealth();
    }

    protected virtual void Update()
    {
        SynchronizedLayerIndex();
        layerIndex = floorAgent.currentFloorIndex;
        currentHealth = health.CurrentHealth;
        
        if(attackRange > viewDistance)
            attackRange =  viewDistance;
    }

    // =========================
    // PATHFINDING
    // =========================

    #region PATHFINDING
    private static readonly Vector3Int[] kDirs = new Vector3Int[]
    {
        new Vector3Int( 1, 0, 0),
        new Vector3Int(-1, 0, 0)
        /*,
        new Vector3Int( 0, 1, 0),
        new Vector3Int( 0,-1, 0),*/
    };
    
    private static readonly Vector3Int[] OrthogonalDirs = new Vector3Int[]
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.left,
        Vector3Int.right
    };

    public List<Vector3Int> BuildOrthogonalPerimeterOffsets(ObjectFootprint fp)
    {
        var occupied = new HashSet<Vector3Int>();
        foreach (var c in fp.occupiedCells)
            occupied.Add(new Vector3Int(c.x, c.y, 0));

        var perimeter = new HashSet<Vector3Int>();
        foreach (var cell in occupied)
        {
            foreach (var d in OrthogonalDirs)
            {
                var nb = cell + d;
                if (occupied.Contains(nb)) continue;
                perimeter.Add(nb);
            }
        }
        return new List<Vector3Int>(perimeter);
    }

    private List<Vector3Int> BuildPerimeterNeighborOffsets(ObjectFootprint fp)
    {
        var occupied = new HashSet<Vector3Int>();
        foreach (var c in fp.occupiedCells)
            occupied.Add(new Vector3Int(c.x, c.y, 0));

        var perimeter = new HashSet<Vector3Int>();
        foreach (var cell in occupied)
        {
            foreach (var d in kDirs)
            {
                var nb = cell + d;
                if (occupied.Contains(nb)) continue;
                perimeter.Add(nb);
            }
        }
        return new List<Vector3Int>(perimeter);
    }
    
    public PathFinding FindBestPathToAnyAdjacentWithoutDiagonal(GameObject target, int layerIndex)
{
    if (target == null)
        return null;

    var graph = GraphNode.Instance.layerGraphs[layerIndex];
    var fp = target.GetComponent<ObjectFootprint>();
    var targetPosWorld = Vector3Int.FloorToInt(target.transform.position);
    targetPosWorld.z = 0;

    var neighborOffsets = BuildOrthogonalPerimeterOffsets(fp);
    if (neighborOffsets == null || neighborOffsets.Count == 0)
        return null;

    Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
    currentGridPos.z = 0;

    var sortedValidNeighbors = neighborOffsets
        .Select(off => 
        {
            Vector3Int worldPos = targetPosWorld + off;
            worldPos.z = 0;
            return worldPos;
        })
        .Where(pos => graph.nodes.TryGetValue(pos, out Node node) && node.isWalkable)
        .OrderBy(pos => (pos - currentGridPos).sqrMagnitude) 
        .ToList();

    float bestCost = float.MaxValue;
    PathFinding bestPath = null;
    
    int maxPathsToCheck = 3; 
    int pathsChecked = 0;

    foreach (var neighborWorld in sortedValidNeighbors)
    {
        var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
            currentGridPos, floorAgent.currentFloorIndex,
            neighborWorld, layerIndex);

        if (path == null || path.segments.Count == 0)
            continue;

        if (path.totalCost < bestCost)
        {
            bestCost = path.totalCost;
            bestPath = path;
        }

        pathsChecked++;
        if (pathsChecked >= maxPathsToCheck) 
            break; 
    }

    return bestPath;
}

    public PathFinding FindBestPathToAnyAdjacent(Task task)
    {
        if (task == null || task.targetGameObject == null)
            return null;

        var graph = GraphNode.Instance.layerGraphs[task.layerIndex];
        var fp = task.targetGameObject.GetComponent<ObjectFootprint>();
        var targetPosWorld = Vector3Int.FloorToInt(task.targetGameObject.transform.position);
        targetPosWorld.z = 0;

        var neighborOffsets = BuildPerimeterNeighborOffsets(fp);
        if (neighborOffsets == null || neighborOffsets.Count == 0)
            return null;

        Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        float bestCost = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var off in neighborOffsets)
        {
            Vector3Int neighborWorld = targetPosWorld + off;
            neighborWorld.z = 0;

            if (!graph.nodes.TryGetValue(neighborWorld, out Node node) || !node.isWalkable)
                continue;
            /*Debug.Log($"Start pos: {currentGridPos}, layer: {floorAgent.currentFloorIndex}");
            Debug.Log($"End pos: {neighborWorld}, layer: {task.layerIndex}");
            Debug.Log($"$Current task: {task.taskType}");*/
            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                currentGridPos, floorAgent.currentFloorIndex,
                neighborWorld, task.layerIndex);

            if (path == null || path.segments.Count == 0)
                continue;

            if (path.totalCost < bestCost)
            {
                bestCost = path.totalCost;
                bestPath = path;
            }
        }

        return bestPath;
    }
    
    public PathFinding FindBestPathToAnyAdjacent(GameObject target, int layerIndex)
    {
        if (target == null)
            return null;

        var graph = GraphNode.Instance.layerGraphs[layerIndex];
        var fp = target.GetComponent<ObjectFootprint>();
        var targetPosWorld = Vector3Int.FloorToInt(target.transform.position);
        targetPosWorld.z = 0;

        var neighborOffsets = BuildPerimeterNeighborOffsets(fp);
        if (neighborOffsets == null || neighborOffsets.Count == 0)
            return null;

        Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        float bestCost = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var off in neighborOffsets)
        {
            Vector3Int neighborWorld = targetPosWorld + off;
            neighborWorld.z = 0;

            if (!graph.nodes.TryGetValue(neighborWorld, out Node node) || !node.isWalkable)
                continue;

            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                currentGridPos, floorAgent.currentFloorIndex,
                neighborWorld, layerIndex);

            if (path == null || path.segments.Count == 0)
                continue;

            if (path.totalCost < bestCost)
            {
                bestCost = path.totalCost;
                bestPath = path;
            }
        }
        return bestPath;
    }
    
    public PathFinding FindBestPathToTarget(GameObject target, int layerIndex)
    {
        if (target == null)
            return null;

        var graph = GraphNode.Instance.layerGraphs[layerIndex];

        Vector3Int targetGridPos = Vector3Int.FloorToInt(target.transform.position);
        targetGridPos.z = 0;

        Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        // Kiểm tra ô target có tồn tại và đi được không
        if (!graph.nodes.TryGetValue(targetGridPos, out Node targetNode) || !targetNode.isWalkable)
            return null;

        var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
            currentGridPos, floorAgent.currentFloorIndex,
            targetGridPos, layerIndex);

        if (path == null || path.segments.Count == 0)
            return null;

        return path;
    }
    
    public PathFinding FindBestPathToFront(Task task)
    {
        if (task == null || task.targetGameObject == null)
            return null;

        var graph = GraphNode.Instance.layerGraphs[task.layerIndex];

        var fp = task.targetGameObject.GetComponent<ObjectFootprint>();
        if (fp == null)
            return null;

        Vector3Int targetWorld = Vector3Int.FloorToInt(task.targetGameObject.transform.position);
        targetWorld.z = 0;

        Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        // Front direction cố định
        Vector3Int frontDir = new Vector3Int(0, -1, 0);

        float bestCost = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var cell in fp.occupiedCells)
        {
            Vector3Int localCell = new Vector3Int(cell.x, cell.y, 0);

            // Tính cell phía trước
            Vector3Int frontOffset = localCell + frontDir;

            Vector3Int frontWorld = targetWorld + frontOffset;
            frontWorld.z = 0;

            if (!graph.nodes.TryGetValue(frontWorld, out Node node) || !node.isWalkable)
                continue;

            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                currentGridPos, floorAgent.currentFloorIndex,
                frontWorld, task.layerIndex);

            if (path == null || path.segments.Count == 0)
                continue;

            if (path.totalCost < bestCost)
            {
                bestCost = path.totalCost;
                bestPath = path;
            }
        }

        return bestPath;
    }

    public virtual void MoveToTargetPosition(PathFinding path)
    {
        if (path == null) return;
        
        characterMovement.currentPath = path;
        if (characterMovement.moveCoroutine != null)
        {
            StopCoroutine(characterMovement.moveCoroutine);
        }
        characterMovement.moveCoroutine =
            StartCoroutine(characterMovement.FollowPathCoroutine(path));

        currentState = UnitState.Move;
        animState = AnimState.Moving;
    }
    
    public void MoveDirectlyToTarget(GameObject target)
    {
        if (target == null)
            return;

        var rb = characterMovement.rb;

        Vector2 myPos = rb.position;
        Vector2 targetPos = target.transform.position;

        Vector2 direction = (targetPos - myPos);

        if (direction.magnitude <= 0.05f)
            return;

        direction.Normalize();

        characterMovement.HandleFlipByPosition(targetPos);

        Vector2 nextPos = myPos + direction * characterMovement.moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPos);
    }

    #endregion
    
    // =========================
    // Method
    // =========================

    #region Method
    public bool IsCollidingWithTarget(GameObject target)
    {
        if (target == null )
            return false;

        var currentCol = GetComponent<Collider2D>();
        var targetCol = target.GetComponent<Collider2D>();

        if (currentCol == null || targetCol == null)
            return false;

        ColliderDistance2D dist = currentCol.Distance(targetCol);

        return dist.distance <= 0.05f; 
    }
    public virtual void UpdateFacing(Vector3 dir)
    {
        Vector3 scale = transform.localScale;
        scale.x = dir.x < 0
            ? -Mathf.Abs(scale.x)
            : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
    
    public GameObject SelectClosestTarget(List<GameObject> enemies)
    {
        GameObject best = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                best = enemy;
            }
        }

        return best;
    }
    
    public Vector3Int FindPatrolPosition(Vector3Int buildingCell, int minRadius = 2, int maxRadius = 4)
    {
        const int maxTries = 20;

        for (int i = 0; i < maxTries; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float radius = Random.Range(minRadius, maxRadius + 1);

            Vector2 offset = dir * radius;

            Vector3Int candidate = new Vector3Int(
                buildingCell.x + Mathf.RoundToInt(offset.x),
                buildingCell.y + Mathf.RoundToInt(offset.y),
                0
            );
            
            var node = GraphNode.Instance.GetNode(candidate, assignedBuilding.layerIndex);

            if (node == null)
                continue;

            return candidate;
        }

        return Vector3Int.zero;
    }
    
    public bool IsStopped()
    {
        return currentState != UnitState.Move;
    }

    private void SynchronizedLayerIndex()
    {
        if(floorAgent._currentFloorIndex != characterMovement.CurrentLayer)
            floorAgent.MoveToFloor(characterMovement.CurrentLayer);
    }

    public void StopMove()
    {
        if(characterMovement != null)
            characterMovement.RequestStopMoving();
    }
    
    public void EndAnim()
    {
        animState = AnimState.Idle;
    }

    public void ResetAnim()
    {
        animState = AnimState.Idle;
        currentState = UnitState.Idle;
    }

    public virtual UnitState GetState()
    {
        return UnitState.Idle;
    }
    
    public void ClearAggro()
    {
        currentTarget = null;
        lastSeenPosition = Vector2.zero;
        lastSeenLayerIndex = -1;
    }
    
    private void ChangeTransparent(float cap)
    {
        Color c = spriteRenderer.color;
        c.a = cap;
        spriteRenderer.color = c;
    }
    
    protected void DisableAll()
    {
        var col = GetComponent<Collider2D>();
        col.enabled = false;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void UpdateHealth()
    {
        if (health != null && statsManager != null)
        {
            health.maxHealth = statsManager.MaxHealth;
        }
    }
    
    #region Enemy Method

    public Building FindNearestBuilding(Vector3 currentPosition)
    {
        var closestBuildings = UnitManager.Instance.buildings
            .OrderBy(b => (b.transform.position - currentPosition).sqrMagnitude)
            .Take(5); 

        float minCost = float.MaxValue; 
        Building nearest = null;

        foreach (var building in closestBuildings)
        {
            if(building == null || building.buildingState == BuildingState.Destroyed)
                continue;
            
            var path = FindBestPathToAnyAdjacent(building.gameObject, building.layerIndex);
            if (path == null)
                continue;

            if (path.totalCost < minCost)
            {
                minCost = path.totalCost;
                nearest = building;
            }
        }

        return nearest;
    }

    public List<GameObject> DetectNPCs(float range, Vector2 dir)
    {
        List<GameObject> npcsInRange = new List<GameObject>();

        int size = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            range,
            results,
            LayerMask.GetMask("NPC"));

        dir.Normalize();

        Vector2 myPos = transform.position;

        for (var i = 0; i < size; i++)
        {
            var hit = results[i];
            if (hit == null || !hit.CompareTag("NPC"))
                continue;

            Vector2 dirToNPC = (hit.transform.position - (Vector3)myPos).normalized;
            if (Vector2.Dot(dir, dirToNPC) <= 0)
                continue;

            Bounds b = hit.bounds;
            Vector2[] samplePoints =
            {
                b.center,
                new(b.center.x, b.max.y),
                new(b.center.x, b.min.y),
                new(b.min.x, b.center.y),
                new(b.max.x, b.center.y)
            };

            bool visible = false;

            foreach (var point in samplePoints)
            {
                Vector2 dirRay = point - myPos;
                float dist = dirRay.magnitude;
                dirRay.Normalize();

                var ray = Physics2D.Raycast(
                    myPos,
                    dirRay,
                    dist,
                    obstacleLayer);

                if (ray.collider == null)
                {
                    visible = true;
                    break;
                }
            }

            if (visible)
            {
                npcsInRange.Add(hit.gameObject);
            }
        }

        return npcsInRange;
    }
    
    public EnemyDirection GetDirection(Vector2 from, Vector2 to)
    {
        var dir = to - from;

        if (dir.sqrMagnitude < 0.0001f)
            return EnemyDirection.None;

        dir.Normalize();

        var dirRight = new Vector2(Mathf.Abs(dir.x), dir.y);

        var angle = Vector2.Angle(Vector2.down, dirRight);

        return angle switch
        {
            <= 45f => EnemyDirection.Down,   
            <= 135f => EnemyDirection.Right, 
            _ => EnemyDirection.Up           
        };
    }

    public void EnemyResetState()
    {
        ResetAnim();
        currentTarget = null;
        currentTargetLayerIndex = -1;
    }

    #endregion
    
    #endregion

    #region Attack Flag
    public virtual bool HasTarget()
    {
        return currentTarget != null;
    }

    public virtual void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public virtual void ClearTarget()
    {
        currentTarget = null;
    }
        
    public virtual void StartAttackSignal()
    {
        isAttacking = true;
        isInWindup = true;
    }

    public virtual void EndWindupSignal()
    {
        isInWindup = false;
    }

    public virtual void EndAttackSignal()
    {
        isAttacking = false;
        isInWindup = false;
    }
    

    #endregion

    #region Handle Event
    
    protected virtual void HandleGlobalAlarm(GameObject spottedEnemy, Vector3 spottedPosition)
    {
        if (currentTarget != null) return; 
        if (spottedEnemy == null) return;
        if (CompareTag("Enemy")) return;
        
        if (Vector2.Distance(transform.position, spottedPosition) > hearRange) return;

        lastSeenPosition = spottedPosition;
        lastSeenLayerIndex = spottedEnemy.GetComponentInChildren<FloorAgent>()?._currentFloorIndex ?? 0;
    
        isAlerted = true; 
        aggroTimer = aggroDuration; 
    }
    
    protected virtual void HandleHealthChanged(float current, float max)
    {
        
    }

    protected virtual void HandleTakeDamage(float damage)
    {
        if (gameObject.activeInHierarchy && health.CurrentHealth > 0) 
        {
            if (damageEffectCoroutine != null)
            {
                StopCoroutine(damageEffectCoroutine);
            }
            damageEffectCoroutine = StartCoroutine(DamageEffect());

            if (currentState != UnitState.Dead && !isKnockedBack 
                && Time.time >= lastKnockbackTime + knockbackCooldown)
            {
                if (hitStunCoroutine != null)
                {
                    StopCoroutine(hitStunCoroutine);
                }
                
                lastKnockbackTime = Time.time; 
                hitStunCoroutine = StartCoroutine(HitStunRoutine());
            }
        }
    }

    protected virtual void HandleDeath()
    {
        DisableAll();
        
        currentState = UnitState.Dead;
        animState = AnimState.Dead;
        animFSM.ChangeState(currentState, animState);

        if (assignedBuilding != null)
        {
            assignedBuilding.RemoveUnit(this);
        }

        if (UnitManager.Instance.allUnits.Contains(this))
            UnitManager.Instance.allUnits.Remove(this);
        
        this.enabled = false;
    }
    
    private void HandleLevelUp()
    {
        // Ví dụ: Khi lên cấp thì cập nhật lại Max HP và bơm đầy máu
        // health.SetMaxHealth(statsManager.MaxHealth);
        // health.HealToFull();
    }

    public void Die()
    {
        PoolManager.Instance.Despawn(transform.gameObject);
    }
    
    private IEnumerator DamageEffect()
    {
        spriteRenderer.color = Color.red;
        
        yield return new WaitForSeconds(0.1f);
        
        spriteRenderer.color = Color.white;
        
        damageEffectCoroutine = null; 
    }
    
    
    private IEnumerator HitStunRoutine()
    {
        isKnockedBack = true;

        EndAttackSignal();
        
        StopMove(); 

        currentState = UnitState.Idle;
        animState = AnimState.Idle;
        animFSM.ChangeState(currentState, animState);

        yield return new WaitForSeconds(hitStunDuration);

        isKnockedBack = false;
    }
    #endregion
    

    public abstract void UseSpecialAbility();
    
    protected virtual void OnEnable()
    {
        GlobalAlarmSystem.OnEnemySpotted += HandleGlobalAlarm;
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnTakeDamage += HandleTakeDamage;
            health.OnDie += HandleDeath;
        }
        if (statsManager != null)
        {
            statsManager.OnLevelUp += HandleLevelUp;
        }
        
    }

    protected virtual void OnDisable()
    {
        GlobalAlarmSystem.OnEnemySpotted -= HandleGlobalAlarm;
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnTakeDamage -= HandleTakeDamage;
            health.OnDie -= HandleDeath;
        }
        
        if (statsManager != null)
        {
            statsManager.OnLevelUp -= HandleLevelUp;
        }
    }
    
    
    
}
