using System.Collections;
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
    public Task currentTask = null;
    public Task currentMiniTask = null;

    protected Rigidbody2D rb;
    protected Animator animator;
    public SpriteRenderer spriteRenderer;
    public CharacterMovement characterMovement;
    public Building assignedBuilding;
    public FloorAgent floorAgent;
    public Coroutine executeCoroutine;

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
        #region Move and Check
    public virtual bool MoveToTaskPosition(Task task)
    {
        var graph = GraphNode.Instance.layerGraphs[task.layerIndex];
        var objectFootprint = task.targetGameObject.GetComponent<ObjectFootprint>();
        var targetPosition = task.targetGameObject.transform.position;
/*
        if(currentTask.currentMiniTask != null)
        {
            targetPosition = currentTask.currentMiniTask.targetGameObject.transform.position;
        }*/

        List<Vector3Int> neighborOffsets = null;

        if (objectFootprint.occupiedCells.Count > 1)
        {
            int leftMostX = int.MaxValue;
            int rightMostX = int.MinValue;

            foreach (var cell in objectFootprint.occupiedCells)
            {
                if (cell.y == 0)
                {
                    if (cell.x < leftMostX)
                        leftMostX = cell.x;

                    if (cell.x > rightMostX)
                        rightMostX = cell.x;
                }
            }

            if (leftMostX != int.MaxValue && rightMostX != int.MinValue)
            {
                Vector3Int offsetLeft = new Vector3Int(leftMostX - 1, 0, 0);
                Vector3Int offsetRight = new Vector3Int(rightMostX + 1, 0, 0);

                neighborOffsets = new List<Vector3Int>
                {
                    offsetLeft,
                    offsetRight
                };
            }
        }
        else
        {
            neighborOffsets = new List<Vector3Int>
            {
                new Vector3Int(-1, 0, 0),
                new Vector3Int(1, 0, 0)
            };
        }

        Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        Vector3Int bestNode = Vector3Int.FloorToInt(targetPosition);
        float shortestDistance = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var offset in neighborOffsets)
        {
            Vector3Int neighborPos = Vector3Int.FloorToInt(targetPosition) + offset;
            neighborPos.z = 0;

            graph.nodes.TryGetValue(neighborPos, out Node node);

            if (node == null)
                continue;

            if (node.isWalkable)
            {
                PathFinding path = PathfindingAlgorithm.Instance.FindMultiLayerPath(currentGridPos, floorAgent.currentFloorIndex,
                    neighborPos, task.layerIndex);

                if (neighborPos == currentGridPos)
                {
                    bestPath = new PathFinding();
                    bestPath.totalCost = 0;
                    shortestDistance = 0;
                    break;
                }

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

        if (bestPath == null)
            return false;

        characterMovement.currentPath = bestPath;

        StopAllCoroutines();
        characterMovement.moveCoroutine = StartCoroutine(characterMovement.FollowPathCoroutine(bestPath));
        return true;
    }
    public PathFinding CanMoveToTaskTarget(Task task)
    {
        var graph = GraphNode.Instance.layerGraphs[task.layerIndex];
        var objectFootprint = task.targetGameObject.GetComponent<ObjectFootprint>();
        var targetPosition = task.targetGameObject.transform.position;
/*
        if (task.currentMiniTask != null)
        {
            targetPosition = task.currentMiniTask.targetGameObject.transform.position;
        }*/

        List<Vector3Int> neighborOffsets = null;

        if (objectFootprint.occupiedCells.Count > 1)
        {
            int leftMostX = int.MaxValue;
            int rightMostX = int.MinValue;

            foreach (var cell in objectFootprint.occupiedCells)
            {
                if (cell.y == 0)
                {
                    if (cell.x < leftMostX)
                        leftMostX = cell.x;

                    if (cell.x > rightMostX)
                        rightMostX = cell.x;
                }
            }

            if (leftMostX != int.MaxValue && rightMostX != int.MinValue)
            {
                Vector3Int offsetLeft = new Vector3Int(leftMostX - 1, 0, 0);
                Vector3Int offsetRight = new Vector3Int(rightMostX + 1, 0, 0);

                neighborOffsets = new List<Vector3Int>
            {
                offsetLeft,
                offsetRight
            };
            }
            else
            {
                neighborOffsets = new List<Vector3Int>
            {
                new Vector3Int(-1, 0, 0),
                new Vector3Int(1, 0, 0)
            };
            }
        }
        else
        {
            neighborOffsets = new List<Vector3Int>
        {
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };
        }

        Vector3Int currentGridPos = Vector3Int.FloorToInt(transform.position);
        currentGridPos.z = 0;

        Vector3Int bestNode = Vector3Int.FloorToInt(targetPosition);
        float shortestDistance = float.MaxValue;
        PathFinding bestPath = null;

        foreach (var offset in neighborOffsets)
        {
            Vector3Int neighborPos = Vector3Int.FloorToInt(targetPosition) + offset;
            neighborPos.z = 0;

            if (neighborPos == currentGridPos)
            {
                bestPath = new PathFinding();
                bestPath.totalCost = 0;
                shortestDistance = 0;
                break;
            }

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

        if (bestPath != null )
            return bestPath;

        return null;
    }
    #endregion
        #region Execute Task
    public void ExecuteTask()
    {
        if (currentTask == null || currentTask.taskStatus == TaskStatus.Completed)
            return;

        executeCoroutine = StartCoroutine(ExecuteTask(currentTask));
    }

    private IEnumerator ExecuteTask(Task task)
    {
        if (task.HasUnfinishedMiniTasks())
        {
            if (task.currentMiniTask == null)
            {
                task.TryAdvanceMiniTask();
            }

            Task activeTask = task.currentMiniTask;

            if (activeTask != null)
            {
                Debug.Log($"[Unit] Executing mini task: {activeTask.taskType} at {activeTask.targetGameObject.transform.position}");

                var canExecute = MoveToTaskPosition(activeTask);
                if (!canExecute)
                {
                    if (!activeTask.isInPendingQueue && activeTask == task)
                    {
                        TaskManager.Instance.pendingTask.Enqueue(activeTask);
                        activeTask.isInPendingQueue = true;
                    }
                    currentTask = null;
                    currentState = UnitState.Idle;
                    yield break;
                }
                task.taskStatus = TaskStatus.InProgress;
                activeTask.taskStatus = TaskStatus.InProgress;
                currentState = UnitState.Working;
                targetDestination = activeTask.targetGameObject.transform;
                currentMiniTask = activeTask;
                task.currentMiniTask = null;

            }
        }

        if (task.miniTasks.Count == 0 && task.currentMiniTask == null)
        {
            Debug.Log($"[Unit] All mini tasks completed. Executing main task: {task.taskType} at {task.targetGameObject.transform.position}");

            var canExecuteMainTask = MoveToTaskPosition(currentTask);
            if (!canExecuteMainTask)
            {
                if (!task.isInPendingQueue)
                {
                    TaskManager.Instance.pendingTask.Enqueue(task);
                    task.isInPendingQueue = true;
                }
                currentTask = null;
                currentState = UnitState.Idle;
                yield break;
            }

            task.taskStatus = TaskStatus.InProgress;
            currentState = UnitState.Working;
            targetDestination = task.targetGameObject.transform;

        }
    }

    public IEnumerator DelayContinueExecuteTask()
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        ExecuteTask();
    }
    #endregion
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