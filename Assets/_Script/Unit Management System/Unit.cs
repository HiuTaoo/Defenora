using System;
using System.Collections.Generic;
using System.Linq;
using _Script.BT;
using _Script.BT.GlobalAlarm;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    public string unitName;
    public UnitType unitType;
    public UnitState currentState = UnitState.Idle;
    public AnimState animState = AnimState.Idle;

    [Header("Stats")]
    public float health = 100f;
    public float maxHealth = 100f;

    [Header("Movement")]
    public Transform targetDestination;
    public float stoppingDistance = 0.1f;

    [Header("Task")]
    public Task currentTask;
    public bool IsBusy => currentTask != null && !currentTask.IsCompleted  && currentTask.targetGameObject != null;

    [Header("Deployment")]
    public Building assignedBuilding;  
    
    [Header("Layer")]
    public int obstacleLayer;
    public int enemyLayer;
    
    [Header("Enemy")]
    public Collider2D[] results;
    
    [Header("Aggro")]
    public float aggroTimer;
    public float aggroDuration = 5f;
    public Transform currentTarget;
    public Vector2 lastSeenPosition;
    public int lastSeenLayerIndex;
    
    [Header("Sensor")]
    public float detectTimer = 0f;
    public float detectInterval = 0.25f;

    protected BehaviourTree bt;

    private Rigidbody2D rb;
    protected Animator animator;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public CharacterMovement characterMovement;
    [HideInInspector] public FloorAgent floorAgent;

    public System.Action<Unit> OnUnitDestroyed;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterMovement = GetComponentInChildren<CharacterMovement>();
        floorAgent = GetComponentInChildren<FloorAgent>();
                        
        enemyLayer = LayerMask.GetMask("NPC");
        obstacleLayer = LayerMask.GetMask("VisionBlocker");
        results = new Collider2D[20];

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        unitName = gameObject.name;
    }

    private void Update()
    {
        SynchronizedLayerIndex();
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
    public virtual bool MoveToTargetPosition()
    {
        var path = FindBestPathToAnyAdjacent(currentTask);
        if (path == null)
            return false;

        characterMovement.currentPath = path;
        StopAllCoroutines();
        characterMovement.moveCoroutine =
            StartCoroutine(characterMovement.FollowPathCoroutine(path));

        currentState = UnitState.Move;
        animState = AnimState.Moving;
        return true;
    }
    
    public virtual void MoveToTargetPosition(PathFinding path)
    {
        if (path == null) return;
        
        characterMovement.currentPath = path;
        StopAllCoroutines();
        characterMovement.moveCoroutine =
            StartCoroutine(characterMovement.FollowPathCoroutine(path));

        currentState = UnitState.Move;
        animState = AnimState.Moving;
    }
    
    public bool IsCollidingWithTarget(GameObject target)
    {
        if (target == null )
            return false;

        var pawnCol = GetComponent<CircleCollider2D>();
        var targetCol = target.GetComponent<Collider2D>();

        if (pawnCol == null || targetCol == null)
            return false;

        ColliderDistance2D dist = pawnCol.Distance(targetCol);

        return dist.distance <= 0.05f; 
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
    
    protected virtual void OnEnable()
    {
        // Đăng ký nghe loa phát thanh khi Unit được bật/sinh ra
        GlobalAlarmSystem.OnEnemySpotted += HandleGlobalAlarm;
    }

    protected virtual void OnDisable()
    {
        // Hủy đăng ký khi Unit chết/tắt để tránh lỗi tràn bộ nhớ (Memory Leak)
        GlobalAlarmSystem.OnEnemySpotted -= HandleGlobalAlarm;
    }

    // Hàm phản ứng lại báo động
    protected virtual void HandleGlobalAlarm(GameObject spottedEnemy, Vector3 spottedPosition)
    {
        // Nếu mình đang đánh nhau với thằng khác rồi thì bơ đi, không quan tâm
        if (currentTarget != null) return;

        // Nếu kẻ địch đã chết hoặc biến mất thì bỏ qua
        if (spottedEnemy == null) return;

        // TÍNH TOÁN KHOẢNG CÁCH: Chỉ báo động nếu tiếng la hét nằm trong phạm vi nghe thấy
        // (Ví dụ: Lính ở bên kia bản đồ thì không thể nghe thấy lính bên này la lên được)
        float hearRange = 15f; 
        if (Vector2.Distance(transform.position, spottedPosition) > hearRange) return;

        // ----- HÀNH ĐỘNG KHI NGHE BÁO ĐỘNG -----
        // Đánh thức giác quan: Gán vị trí cuối cùng nhìn thấy địch
        lastSeenPosition = spottedPosition;
        
        // Gán mục tiêu để các nhánh Behavior Tree tự động chuyển sang chế độ Cảnh giác
        currentTarget = spottedEnemy.transform; 
        
        // Cập nhật lại timer báo động
        aggroTimer = aggroDuration; 

    }
    
    public void EndAnim()
    {
        animState = AnimState.Idle;
    }

    public virtual UnitState GetState()
    {
        return UnitState.Idle;
    }

    #endregion
    
    
    // =========================
    // LIFE
    // =========================

    #region LIFE
    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        health = Mathf.Clamp(health, 0, maxHealth);

        if (health <= 0)
            Die();
    }

    protected virtual void Die()
    {
        OnUnitDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
    #endregion
    

    public abstract void UseSpecialAbility();
}
