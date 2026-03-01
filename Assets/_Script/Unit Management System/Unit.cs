using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    public string unitName;
    public UnitType unitType;
    public UnitState currentState = UnitState.Idle;

    [Header("Stats")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float attackDamage = 10f;
    public float attackRange = 2f;

    [Header("Movement")]
    public Transform targetDestination;
    public float stoppingDistance = 0.1f;

    [Header("Task")]
    public Task currentTask;
    public bool IsBusy => currentTask != null && !currentTask.IsCompleted  && currentTask.targetGameObject != null;

    [Header("Deployment")]
    public Building assignedBuilding;   
    
    protected Rigidbody2D rb;
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

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        unitName = gameObject.name;
    }

    protected virtual void Update()
    {
        //UpdateFacing();
    }

    // =========================
    // PATHFINDING
    // =========================

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

        currentState = UnitState.Moving;
        return true;
    }
    
    public virtual void MoveToTargetPosition(PathFinding path)
    {
        if (path == null) return;
        
        characterMovement.currentPath = path;
        StopAllCoroutines();
        characterMovement.moveCoroutine =
            StartCoroutine(characterMovement.FollowPathCoroutine(path));

        currentState = UnitState.Moving;
    }


    // =========================
    // VISUAL
    // =========================

    public virtual void UpdateFacing()
    {
        if (rb == null || rb.velocity.x == 0)
            return;

        Vector3 scale = transform.localScale;
        scale.x = rb.velocity.x < 0
            ? -Mathf.Abs(scale.x)
            : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
    
    // =========================
    // LIFE
    // =========================

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

    public abstract void UseSpecialAbility();
}
