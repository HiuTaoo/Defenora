using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

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
    public Task currentTask = null;

    protected Rigidbody2D rb;
    protected Animator animator;
    public SpriteRenderer spriteRenderer;
    public CharacterMovement characterMovement;
    public Building assignedBuilding;
    public FloorAgent floorAgent;

    public System.Action<Unit> OnUnitDestroyed;
    public System.Action<Unit> OnDestinationReached;
    public System.Action<Unit> OnUnitIdle;

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
        UpdateAnimations();
    }

    #region Execute Task Logic
    public virtual bool MoveToTaskPosition(Vector3Int position, int layer)
    {
        var graph = GraphNode.Instance.layerGraphs[layer];
        List<Vector3Int> neighborOffsets = new List<Vector3Int>
        {
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };

        Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        Vector3Int bestNode = position;
        float shortestDistance = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var offset in neighborOffsets)
        {
            Vector3Int neighborPos = position + offset;
            neighborPos.z = 0;
            graph.nodes.TryGetValue(neighborPos, out Node node);

            if(node == null)
                continue;

            if (node.isWalkable)
            {
                PathFinding path = PathfindingAlgorithm.Instance.FindMultiLayerPath(currentGridPos, floorAgent.currentFloorIndex, neighborPos, layer);

                if(path.segments.Count == 0)
                {
                    continue;
                }

                if (path.segments.Count != 0 && path.totalCost < shortestDistance)
                {
                    bestNode = neighborPos;
                    shortestDistance = path.totalCost;
                    bestPath = path;
                }
            }
        }

        if (bestPath == null)
        {
            Debug.LogWarning($"Không tìm được đường đi hợp lệ đến bất kỳ node lân cận nào quanh {position}");
            return false;
        }

        characterMovement.currentPath = bestPath;

        StopAllCoroutines();
        characterMovement.moveCoroutine = StartCoroutine(characterMovement.FollowPathCoroutine(bestPath));
        return true;
    }

    public PathFinding CanMoveToTaskTarget(Task task)
    {
        var graph = GraphNode.Instance.layerGraphs[task.layerIndex];
        var objectFootprint = task.targetGameObject.GetComponent<ObjectFootprint>();

        List<Vector3Int> neighborOffsets = null;

        if (objectFootprint.occupiedCells.Count == 1)

            neighborOffsets = new List<Vector3Int>
        {
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };

        Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        Vector3Int bestNode = Vector3Int.FloorToInt(task.targetGameObject.transform.position);
        float shortestDistance = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var offset in neighborOffsets)
        {
            Vector3Int neighborPos = Vector3Int.FloorToInt(task.targetGameObject.transform.position) + offset;
            neighborPos.z = 0;
            graph.nodes.TryGetValue(neighborPos, out Node node);

            if (node == null)
                continue;

            if (node.isWalkable)
            {
                PathFinding path = PathfindingAlgorithm.Instance.FindMultiLayerPath(currentGridPos, floorAgent.currentFloorIndex,
                    neighborPos, task.layerIndex);

                if (path.segments.Count == 0)
                    continue;

                if (path != null && path.totalCost < shortestDistance)
                {
                    bestNode = neighborPos;
                    shortestDistance = path.totalCost;
                    bestPath = path;
                }
            }
        }
        if (bestPath != null && shortestDistance > 0)
            return bestPath;

        return null;
    }

    public void ExecuteTask()
    {
        if (currentTask != null && currentTask.targetGameObject != null && currentTask.taskStatus == TaskStatus.NotStarted)
        {
            StartCoroutine(ExecuteTask(currentTask));
        }
    }

    public IEnumerator ExecuteTask(Task task)
    {
        yield return new WaitForSeconds(0.1f);

        var canExecuteTask = MoveToTaskPosition(Vector3Int.FloorToInt(task.targetGameObject.transform.position), task.layerIndex);
        if (!canExecuteTask)
        {
            UnitManager.Instance.CleanupTaskFromInProgress(task, this);

            bool taskAlreadyInPending = false;
            lock (TaskManager.Instance.pendingTask)
            {
                var tempList = TaskManager.Instance.pendingTask.ToArray();
                taskAlreadyInPending = System.Array.Exists(tempList, t => t == task);
            }

            if (!taskAlreadyInPending)
            {
                TaskManager.Instance.pendingTask.Enqueue(task);
                Debug.Log($"Task {task.taskType} cannot be executed by {unitName}. Added to pending tasks.");
            }
            else
            {
                Debug.Log($"Task {task.taskType} is already in pending queue. Skipping enqueue.");
            }

            currentTask = null;
            currentState = UnitState.Idle;

            yield break;
        }
        else
        {
            task.taskStatus = TaskStatus.InProgress;
            currentState = UnitState.Working;
            targetDestination = task.targetGameObject.transform;
        }
    }
    #endregion

    public virtual void StopMovement()
    {
        targetDestination = null;
        rb.velocity = Vector2.zero;
        currentState = UnitState.Idle;
    }

    protected virtual void HandleMovement()
    {
        if (targetDestination == null || currentState != UnitState.Moving)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector3 direction = (targetDestination.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetDestination.position);

        if (distance <= stoppingDistance)
        {
            rb.velocity = Vector2.zero;
            currentState = UnitState.Stationed;
            OnDestinationReached?.Invoke(this);

            if (targetDestination.name.Contains("_Target"))
                Destroy(targetDestination.gameObject);
        }
        else
        {
            rb.velocity = direction * moveSpeed;
        }
    }

    protected virtual void UpdateAnimations()
    {
        if (animator == null) return;

        if (rb.velocity.x != 0)
        {
            Vector3 scale = transform.localScale;

            if (rb.velocity.x < 0)
                scale.x = -Mathf.Abs(scale.x);
            else
                scale.x = Mathf.Abs(scale.x);

            transform.localScale = scale;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        health = Mathf.Clamp(health, 0, maxHealth);

        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    protected virtual void Die()
    {
        OnUnitDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    public abstract void UseSpecialAbility();

    public virtual UnitData GetUnitInfo()
    {
        return new UnitData
        {
            unitName = this.unitName,
            unitType = this.unitType,
            currentState = this.currentState,
            health = this.health,
            maxHealth = this.maxHealth,
            position = transform.position
        };
    }
}