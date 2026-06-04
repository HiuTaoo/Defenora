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

public abstract class Unit : MonoBehaviour, IPoolable
{
    [Header("Unit Info")] public string unitName;

    public UnitType unitType;
    public UnitState currentState = UnitState.Idle;
    public AnimState animState = AnimState.Idle;
    public int layerIndex;
    public float currentHealth;

    [Header("Deployment")] public Building assignedBuilding;

    [Header("Layer")] [HideInInspector] public int obstacleLayer;

    [HideInInspector] public int enemyLayer;

    [Header("Array Non Alloc")] 
    public Collider2D[] results;

    public Collider2D[] animalResult;

    [Header("Target")] public Transform currentTarget;

    public int currentTargetLayerIndex;
    public Vector2 lastSeenPosition;
    public int lastSeenLayerIndex;
    [HideInInspector] public bool isAlerted;
    public float aggroTimer;
    public float aggroDuration = 5f;

    [Header("Sensor")] public float detectTimer;

    public float detectInterval = 0.25f;
    public float hearRange = 15f;

    [Header("Animation FSM")] [HideInInspector]
    public AnimationFSM animFSM;

    [Header("Stats System")] [HideInInspector]
    public UnitStatsManager unitStatsManager;

    public GameObject enemySpawnPoint;
    [HideInInspector] public DynamicSortingYX sortingYX;
    [HideInInspector] public Health health;

    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public CharacterMovement characterMovement;
    [HideInInspector] public FloorAgent floorAgent;

    [Header("Combat Stats")] public float attackRange;

    public float lastAttackTime = -999f;
    public bool isKnockedBack;
    public float hitStunDuration = 0.1f;
    public float knockbackCooldown = 3f;

    [Header("Vision")] [Range(0, 360)] public float viewAngle;

    protected BehaviourTree bt;

    [Header("Effects")] private Coroutine damageEffectCoroutine;
    private Coroutine hitStunCoroutine;
    private float lastKnockbackTime = -999f;

    [Header("Unstuck System")] [SerializeField]
    private float stuckCheckInterval = 5.0f;

    private Vector3 _lastPosition;
    private float _stuckTimer;

    public Action<Unit> OnUnitDestroyed;
    private Rigidbody2D rb;

    public float attackDamage => unitStatsManager != null ? unitStatsManager.AttackDamage : 0;
    public float viewDistance => unitStatsManager != null ? unitStatsManager.ViewDistance : 0;
    public float attackCooldown => unitStatsManager != null ? unitStatsManager.AttackCooldown : 0;
    public bool isAttacking { get; protected set; }
    public bool isInWindup { get; protected set; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterMovement = GetComponentInChildren<CharacterMovement>();
        floorAgent = GetComponentInChildren<FloorAgent>();
        animFSM = GetComponent<AnimationFSM>();
        sortingYX = GetComponent<DynamicSortingYX>();
        health = GetComponentInChildren<Health>();
        unitStatsManager = GetComponentInChildren<UnitStatsManager>();

        enemyLayer = LayerMask.GetMask("NPC");
        obstacleLayer = LayerMask.GetMask("VisionBlocker");
        results = new Collider2D[20];

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        unitName = gameObject.name;
        UpdateHealth();
    }

    /*private void Start()
    {
        health.maxHealth = unitStatsManager.MaxHealth;
        health.SetCurrentHealth(unitStatsManager.MaxHealth);
    }*/

    protected virtual void Update()
    {
        SynchronizedLayerIndex();
        layerIndex = floorAgent.currentFloorIndex;
        currentHealth = health.CurrentHealth;

        if (this is Archer archer)
        {
            if (archer.isStationed)
                return;
        }
        HandleGridUnstuck();
    }

    //public void SetId(string newId) => id = newId;

    protected virtual void OnEnable()
    {
        GlobalAlarmSystem.OnEnemySpotted += HandleGlobalAlarm;
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnTakeDamage += HandleTakeDamage;
            health.OnDie += HandleDeath;
        }

        if (unitStatsManager != null) unitStatsManager.OnLevelUp += HandleLevelUp;
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

        if (unitStatsManager != null) unitStatsManager.OnLevelUp -= HandleLevelUp;
    }


    public abstract void UseSpecialAbility();

    public virtual List<(string name, string value)> GetSpecialStats()
    {
        return null;
    }

    // =========================
    // PATHFINDING
    // =========================

    #region PATHFINDING

    private static readonly Vector3Int[] kDirs =
    {
        new(1, 0, 0),
        new(-1, 0, 0)
        /*,
        new Vector3Int( 0, 1, 0),
        new Vector3Int( 0,-1, 0),*/
    };

    private static readonly Vector3Int[] OrthogonalDirs =
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
        foreach (var d in OrthogonalDirs)
        {
            var nb = cell + d;
            if (occupied.Contains(nb)) continue;
            perimeter.Add(nb);
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
        foreach (var d in kDirs)
        {
            var nb = cell + d;
            if (occupied.Contains(nb)) continue;
            perimeter.Add(nb);
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

        var currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        var sortedValidNeighbors = neighborOffsets
            .Select(off =>
            {
                var worldPos = targetPosWorld + off;
                worldPos.z = 0;
                return worldPos;
            })
            .Where(pos => graph.nodes.TryGetValue(pos, out var node) && node.isWalkable)
            .OrderBy(pos => (pos - currentGridPos).sqrMagnitude)
            .ToList();

        var bestCost = float.MaxValue;
        PathFinding bestPath = null;

        var maxPathsToCheck = 3;
        var pathsChecked = 0;

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

        var currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        var bestCost = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var off in neighborOffsets)
        {
            var neighborWorld = targetPosWorld + off;
            neighborWorld.z = 0;

            if (!graph.nodes.TryGetValue(neighborWorld, out var node) || !node.isWalkable)
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

        var currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        var bestCost = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var off in neighborOffsets)
        {
            var neighborWorld = targetPosWorld + off;
            neighborWorld.z = 0;

            if (!graph.nodes.TryGetValue(neighborWorld, out var node) || !node.isWalkable)
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

        var targetGridPos = Vector3Int.FloorToInt(target.transform.position);
        targetGridPos.z = 0;

        var currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        // Kiểm tra ô target có tồn tại và đi được không
        if (!graph.nodes.TryGetValue(targetGridPos, out var targetNode) || !targetNode.isWalkable)
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

        var targetWorld = Vector3Int.FloorToInt(task.targetGameObject.transform.position);
        targetWorld.z = 0;

        var currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        // Front direction cố định
        var frontDir = new Vector3Int(0, -1, 0);

        var bestCost = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var cell in fp.occupiedCells)
        {
            var localCell = new Vector3Int(cell.x, cell.y, 0);

            // Tính cell phía trước
            var frontOffset = localCell + frontDir;

            var frontWorld = targetWorld + frontOffset;
            frontWorld.z = 0;

            if (!graph.nodes.TryGetValue(frontWorld, out var node) || !node.isWalkable)
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
        if (characterMovement.moveCoroutine != null) StopCoroutine(characterMovement.moveCoroutine);
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

        var myPos = rb.position;
        Vector2 targetPos = target.transform.position;

        var direction = targetPos - myPos;

        if (direction.magnitude <= 0.05f)
            return;

        direction.Normalize();

        characterMovement.HandleFlipByPosition(targetPos);

        var nextPos = myPos + direction * characterMovement.moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPos);
    }

    #endregion

    // =========================
    // Method
    // =========================

    #region Method

    public bool IsCollidingWithTarget(GameObject target)
    {
        if (target == null)
            return false;

        var currentCol = GetComponent<Collider2D>();
        var targetCol = target.GetComponent<Collider2D>();

        if (currentCol == null || targetCol == null)
            return false;

        var dist = currentCol.Distance(targetCol);

        return dist.distance <= 0.05f;
    }

    public virtual void UpdateFacing(Vector3 dir)
    {
        var scale = transform.localScale;
        scale.x = dir.x < 0
            ? -Mathf.Abs(scale.x)
            : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    public GameObject SelectClosestTarget(List<GameObject> enemies)
    {
        GameObject best = null;
        var minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            var dist = Vector2.Distance(transform.position, enemy.transform.position);

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

        for (var i = 0; i < maxTries; i++)
        {
            var angle = Random.Range(0f, Mathf.PI * 2);
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float radius = Random.Range(minRadius, maxRadius + 1);

            var offset = dir * radius;

            var candidate = new Vector3Int(
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
        if (floorAgent._currentFloorIndex != characterMovement.CurrentLayer)
            floorAgent.MoveToFloor(characterMovement.CurrentLayer);
    }

    public void StopMove()
    {
        if (characterMovement != null)
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

    public BehaviourTree GetBT()
    {
        return bt;
    }

    public void ClearAggro()
    {
        currentTarget = null;
        lastSeenPosition = Vector2.zero;
        lastSeenLayerIndex = -1;
    }

    private void ChangeTransparent(float cap)
    {
        var c = spriteRenderer.color;
        c.a = cap;
        spriteRenderer.color = c;
    }

    protected void DisableAll()
    {
        var col = GetComponent<Collider2D>();
        col.enabled = false;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
    }

    private void UpdateHealth()
    {
        if (health != null && unitStatsManager != null) health.maxHealth = unitStatsManager.MaxHealth;
    }
    
    private void HandleGridUnstuck()
    {
        if (GraphNode.Instance == null) return;

        if (currentState != UnitState.Idle && currentState != UnitState.Move)
        {
            _stuckTimer = 0f;
            _lastPosition = transform.position;
            return;
        }

        if (Vector3.Distance(transform.position, _lastPosition) > 0.05f)
        {
            _lastPosition = transform.position;
            _stuckTimer = 0f;
            return;
        }

        _stuckTimer += Time.deltaTime;

        if (_stuckTimer >= stuckCheckInterval)
        {
            var currentLayer = layerIndex;
            var gridX = Mathf.FloorToInt(transform.position.x);
            var gridY = Mathf.FloorToInt(transform.position.y);
            var currentGridPos = new Vector3Int(gridX, gridY, 0);

            var currentNode = GraphNode.Instance.GetNode(currentGridPos, currentLayer);

            if (currentNode != null && currentNode.isWalkable)
            {
                _stuckTimer = 0f;
                _lastPosition = transform.position;
                return;
            }

            _stuckTimer = 0f;
            _lastPosition = transform.position;

            Debug.LogWarning(
                $"[Unstuck Lưới Mới] Phát hiện [{unitType}] {gameObject.name} đứng im tại ô CẤM ĐI {currentGridPos} quá {stuckCheckInterval}s! Bắt đầu chạy bộ loang rộng bán kính để tìm ô trống...");

            var targetFound = false;
            var bestTargetGrid = Vector3Int.zero;

            const int maxRadiusSearch = 5;
            for (var r = 1; r <= maxRadiusSearch; r++)
            {
                var candidatesAtRadius = new List<Vector3Int>();

                for (var xOffset = -r; xOffset <= r; xOffset++)
                {
                    for (var yOffset = -r; yOffset <= r; yOffset++)
                    {
                        if (Mathf.Abs(xOffset) == r || Mathf.Abs(yOffset) == r)
                        {
                            var checkPos = currentGridPos + new Vector3Int(xOffset, yOffset, 0);
                            var node = GraphNode.Instance.GetNode(checkPos, currentLayer);

                            if (node != null && node.isWalkable) candidatesAtRadius.Add(checkPos);
                        }
                    }
                }

                if (candidatesAtRadius.Count > 0)
                {
                    candidatesAtRadius.Sort((a, b) =>
                        Vector3.Distance(transform.position, new Vector3(a.x + 0.5f, a.y + 0.5f, 0))
                            .CompareTo(Vector3.Distance(transform.position, new Vector3(b.x + 0.5f, b.y + 0.5f, 0)))
                    );

                    bestTargetGrid = candidatesAtRadius[0];
                    targetFound = true;
                    break;
                }
            }

            if (targetFound)
            {
                var targetWorldPos =
                    new Vector3(bestTargetGrid.x + 0.5f, bestTargetGrid.y + 0.5f, transform.position.z);

                transform.position = targetWorldPos;
                _lastPosition = targetWorldPos;

                bt?.ClearState();

                if (currentState == UnitState.Move)
                    currentState = UnitState.Idle;

                Debug.Log(
                    $"[Unstuck Thành Công] Đã giải cứu [{unitType}] {gameObject.name} ra khỏi vùng kẹt thành công sang ô trống lớp bán kính mới: {bestTargetGrid}");
            }
            else
            {
                Debug.LogError(
                    $"[Unstuck Thất Bại] Đã quét nới rộng đến {maxRadiusSearch} ô xung quanh vị trí {currentGridPos} nhưng không tìm thấy bất kỳ ô trống nào!");
            }
        }
    }
    
    #region Enemy Method

    public Building FindNearestBuilding(Vector3 currentPosition)
    {
        var closestBuildings = UnitManager.Instance.buildings
            .OrderBy(b => (b.transform.position - currentPosition).sqrMagnitude)
            .Take(5);

        var minCost = float.MaxValue;
        Building nearest = null;

        foreach (var building in closestBuildings)
        {
            if (building == null || building.buildingState != BuildingState.Completed)
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
    
    public Building FindRandomBuilding(Vector3 currentPosition)
    {
        var closestBuildings = UnitManager.Instance.buildings
            .OrderBy(b => (b.transform.position - currentPosition).sqrMagnitude)
            .Take(5);

        List<Building> validBuildings = new List<Building>();

        foreach (var building in closestBuildings)
        {
            if (building == null || building.buildingState != BuildingState.Completed)
                continue;

            var path = FindBestPathToAnyAdjacent(building.gameObject, building.layerIndex);
            if (path == null)
                continue;

            validBuildings.Add(building);
        }

        if (validBuildings.Count > 0)
        {
            int randomIndex = Random.Range(0, validBuildings.Count);
        
            Debug.Log($"[AI Random] Quét được {validBuildings.Count} nhà hợp lệ ở gần. Đã bốc ngẫu nhiên công trình: {validBuildings[randomIndex].gameObject.name}");
            return validBuildings[randomIndex];
        }

        return null;
    }

    public List<GameObject> DetectNPCs(float range, Vector2 dir)
    {
        var npcsInRange = new List<GameObject>();

        var size = Physics2D.OverlapCircleNonAlloc(
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

            var b = hit.bounds;
            Vector2[] samplePoints =
            {
                b.center,
                new(b.center.x, b.max.y),
                new(b.center.x, b.min.y),
                new(b.min.x, b.center.y),
                new(b.max.x, b.center.y)
            };

            var visible = false;

            foreach (var point in samplePoints)
            {
                var dirRay = point - myPos;
                var dist = dirRay.magnitude;
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

            if (visible) npcsInRange.Add(hit.gameObject);
        }

        return npcsInRange;
    }

    public GameObject DetectPlayer(float range, Vector2 dir)
    {
        if (PlayerController.Instance == null) return null;

        var playerObj = PlayerController.Instance.gameObject;
        Vector2 myPos = transform.position;
        Vector2 playerPos = playerObj.transform.position;

        var sqrDist = (playerPos - myPos).sqrMagnitude;
        if (sqrDist > range * range) return null;

        dir.Normalize();

        var dirToPlayer = (playerPos - myPos).normalized;

        var angle = Vector2.Angle(dir, dirToPlayer);
        if (angle > viewAngle / 2f) return null;

        var playerCollider = playerObj.GetComponentInParent<Collider2D>();
        if (playerCollider == null) return null;

        var b = playerCollider.bounds;
        Vector2[] samplePoints =
        {
            b.center,
            new(b.center.x, b.max.y),
            new(b.center.x, b.min.y),
            new(b.min.x, b.center.y),
            new(b.max.x, b.center.y)
        };

        var visible = false;

        foreach (var point in samplePoints)
        {
            var dirRay = point - myPos;
            var dist = dirRay.magnitude;
            dirRay.Normalize();

            var ray = Physics2D.Raycast(myPos, dirRay, dist, obstacleLayer);

            if (ray.collider == null)
            {
                visible = true;
                break;
            }
        }

        return visible ? playerObj : null;
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

    public List<GameObject> DetectAllNPCsInRange(float range)
    {
        var npcs = new List<GameObject>();
        var size = Physics2D.OverlapCircleNonAlloc(transform.position, range, results,
            LayerMask.GetMask("NPC"));

        for (var i = 0; i < size; i++)
        {
            var hit = results[i];
            if (hit != null && hit.CompareTag("NPC")) npcs.Add(hit.gameObject);
        }

        return npcs;
    }

    public bool CheckTargetBuildingInAttackRange()
    {
        if (currentTarget == null)
            return false;

        var building = currentTarget;

        var buildingCol = building.GetComponent<Collider2D>();
        if (buildingCol == null || buildingCol.isTrigger)
            return false;

        var closest = buildingCol.ClosestPoint(transform.position);

        var dist = Vector2.Distance(transform.position, closest);

        return dist <= attackRange * 0.75;
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
            if (damageEffectCoroutine != null) StopCoroutine(damageEffectCoroutine);
            damageEffectCoroutine = StartCoroutine(DamageEffect());

            if (currentState != UnitState.Dead && !isKnockedBack
                                               && Time.time >= lastKnockbackTime + knockbackCooldown)
            {
                if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);

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

        if (assignedBuilding != null) assignedBuilding.RemoveUnit(this);

        if (UnitManager.Instance.allUnits.Contains(this))
            UnitManager.Instance.allUnits.Remove(this);

        enabled = false;
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

    public void OnSpawned()
    {
        if (unitStatsManager != null) unitStatsManager.CalculateStats();

        if (health != null && unitStatsManager != null) health.SetMaxHealth(unitStatsManager.MaxHealth, true);

        currentState = UnitState.Idle;
        animState = AnimState.Idle;

        _stuckTimer = 0f;
        _lastPosition = transform.position;
    }

    public void OnDespawned()
    {
        _stuckTimer = 0f;
        bt?.ClearState();
    }
}