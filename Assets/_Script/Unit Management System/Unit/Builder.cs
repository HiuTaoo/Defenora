using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.Node.BuilderNode;
using _Script.BT.Node.BuilderNode.Build;
using _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence;
using _Script.BT.Node.BuilderNode.Idle;
using _Script.BT.Node.BuilderNode.RepairStructure;
using _Script.BT.Node.MonkNode.MonkIdle;
using _Script.Object_Pooling;
using _Script.Task;
using _Script.Unit_Management_System.Animation;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public class Builder : Unit
{
    [Header("Builder Info")]
    public Vector2 workBoxSize = new Vector2(1f, 1f);
    public float workRange = 1f;
    
    [Header("Task")]
    public Task currentTask;
    public bool IsBusy => currentTask != null && !currentTask.IsCompleted  && currentTask.targetGameObject != null;
    
    [Header("Carry Item")]
    public ToolType currentTool = ToolType.None;
    public ResourceType currentResource = ResourceType.None;
    
    public BuilderBlackBoard builderBlackBoard { get; private set; }
    
    [Header("Tartget Game Object")]
    public GameObject targetGO;
    
    private IChoppable currentTarget;
    [Header("Inventory")]
    public Inventory currentInventory;

    protected override void Awake()
    {
        base.Awake();
        bt = CreateBuilderBT(this);
        builderBlackBoard = new BuilderBlackBoard();
        currentInventory = GetComponentInChildren<Inventory>();
    }
    
    protected override void Update()
    {
        base.Update();
        bt?.Tick();
        animFSM.ChangeState(currentState, animState);
    }
    
    #region BT
    public BehaviourTree CreateBuilderBT(Builder builder)
    {
        var collectItemSequence = new SequenceNode(
            new IsIdleNode(builder),
            new HasEmptySpaceNode(builder),
            new HasItemAroundNode(builder),
            new CollectItemNode(builder));
        
        var chopTreeSequence = new SequenceNode(
            new IsIdleNode(builder),
            new HasChopTaskNode(builder),
            new FindChopTaskNode(builder),
            new AssignTaskNode(builder),
            new CheckPathToAdjacentTargetNode(builder),
            new MoveToTargetNode(builder),
            new ChopNode(builder)
        );
        
        var transportItemSequence = new SequenceNode(
            new HasItemInInventoryNode(builder),
            new CreateTransportTask(builder),
            new AssignTaskNode(builder),
            new CheckPathToFrontTargetNode(builder),
            new MoveToTargetNode(builder),
            new TransportItemNode(builder)
            );

        var clearObstacleLoop = new RepeatUntilFailureNode(
            new SequenceNode(
                new HasObstacleNode(builder),
                new FindPathToObstacleNode(builder),
                new MoveToObstacleNode(builder),
                new ChopNode(builder)
            )
        );

        var buildStructureSequence = new SequenceNode(
            new IsIdleNode(builder),
            new HasBuildTaskNode(builder),
            new FindBuildTaskNode(builder),
            new AssignTaskNode(builder),
            new CheckPathToAdjacentTargetNode(builder),
            clearObstacleLoop,
            new MoveToTargetNode(builder),
            new BuildNode(builder)
        );

        var repairStuctureSequence = new SequenceNode(
            new IsDawnNode(builder),
            new IsIdleNode(builder),
            new HasBrokenBuildingNode(builder),
            new AssignTaskNode(builder),
            new CheckPathToAdjacentTargetNode(builder),
            new MoveToTargetNode(builder),
            new RepairNode(builder));

        var idleSequence = new SequenceNode(
            new HasIdleTimeNode(builder),
            new MoveFollowAvaiablePathNode(builder),
            new WaitRandomTimeNode(builder));
        
        //Root
        var root = new SelectorNode(
            new SequenceNode(
                new IsInventoryFullNode(builder),
                transportItemSequence
            ),
            repairStuctureSequence,
            collectItemSequence,
            new SelectorNode(
                buildStructureSequence,
                chopTreeSequence
            ),
            transportItemSequence,
            idleSequence
            //new IdleNode(builder)
        );

        return new BehaviourTree(root);
    }
    
    #region Do Task
    public bool IsChopped()
    {
        if (targetGO == null)
            return true;

        Vector3 targetPos = targetGO.transform.position;
        Vector3 dir = targetPos - transform.position;

        if (dir.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x < 0
                ? -Mathf.Abs(scale.x)
                : Mathf.Abs(scale.x);

            transform.localScale = scale;
        }
        
        var target = currentTask.targetGameObject.GetComponent<IChoppable>();
        if (target is Tree)
        {
            var tree = target as Tree;
            if (tree.currentChopHit >= tree.maxChopHit)
            {
                currentTask.taskStatus = TaskStatus.Completed;
                TaskManager.Instance.RemoveTask(currentTask);
                currentTask = null;
                InstaniateObject(PrefabConfig.Instance.woodPrefab, 
                    tree.gameObject.transform.position, tree.layerIndex, 1);
                return true;
            }
        }

        var targetObtacle = builderBlackBoard.currentObstacle;
        
        if (targetObtacle is DecorObject && targetObtacle != null)
        {
            var obj = targetObtacle as DecorObject;
            if (obj.currentChopHit >= obj.maxChopHit)
            {
                builderBlackBoard.currentObstacle = null;
                return true;
            }
        }
        return false;
    }

    public void TryChop()
    {
        var facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        var origin = (Vector2)transform.position + facingDir * workRange;

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, workBoxSize, 0f);
        
        foreach (var hit in hits)
        {
            if (hit?.gameObject == null)
                continue;

            if (hit.CompareTag("Tree") && hit.gameObject == currentTask.targetGameObject)
            {
                currentTarget = hit.gameObject.GetComponent<Tree>();
                break;
            }
            if (hit.CompareTag("Bush") && hit.gameObject.GetComponent<IChoppable>() == builderBlackBoard.currentObstacle)
            {
                currentTarget = hit.gameObject.GetComponent<Bush>();
                break;
            }
            if (hit.CompareTag("Rock") && hit.gameObject.GetComponent<IChoppable>() == builderBlackBoard.currentObstacle)
            {
                currentTarget = hit.gameObject.GetComponent<Rock>();
                break;
            }
        }

        if (currentTarget != null)
        {
            currentTarget.HandleChopped();
        }
    }
    
    public bool IsCompletedBuild()
    {
        if (currentTask == null || currentTask.targetGameObject == null)
            return true;

        Vector3 targetPos = currentTask.targetGameObject.transform.position;
        Vector3 dir = targetPos - transform.position;

        if (dir.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x < 0
                ? -Mathf.Abs(scale.x)
                : Mathf.Abs(scale.x);

            transform.localScale = scale;
        }

        if (currentTask.IsCompleted)
        {
            currentTask = null;
            return true;
        }

        var building = currentTask.targetGameObject.GetComponent<Building>();

        if (building.currentBuildProgress >= 100f)
        {
            currentTask.taskStatus = TaskStatus.Completed;
            TaskManager.Instance.RemoveTask(currentTask);
            return true;
        }

        return false;
    }
    
    public bool IsCompletedRepair()
    {
        if (currentTask == null || currentTask.targetGameObject == null)
            return true;

        Vector3 targetPos = currentTask.targetGameObject.transform.position;
        Vector3 dir = targetPos - transform.position;

        if (dir.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x < 0
                ? -Mathf.Abs(scale.x)
                : Mathf.Abs(scale.x);

            transform.localScale = scale;
        }

        if (currentTask.IsCompleted)
        {
            currentTask = null;
            return true;
        }

        var building = currentTask.targetGameObject.GetComponent<Building>();

        if (building.buildingState == BuildingState.Completed 
            && building.health.CurrentHealth == building.health.maxHealth)
        {
            currentTask.taskStatus = TaskStatus.Completed;
            TaskManager.Instance.RemoveTask(currentTask);
            return true;
        }

        return false;
    }
    
    public void TryBuild()
    {
        if (currentTask == null) return; 

        var facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        var origin = (Vector2)transform.position + facingDir * workRange;

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, workBoxSize, 0f);
    
        foreach (var hit in hits)
        {
            if (hit?.gameObject == null)
                continue;

            if (hit.gameObject != currentTask.targetGameObject)
                continue;
        
            var building = hit.gameObject.GetComponent<Building>();
        
            if (currentTask.taskType == TaskType.BuildStructure)
            {
                if (building != null) 
                {
                    building.HandleBuilt();
                    break;
                }
            }
        
            if (currentTask.taskType == TaskType.RepairStructure)
            {
                var health = hit.gameObject.GetComponentInChildren<Health>();

                if (health != null && building != null) 
                {
                    if (health.CurrentHealth < health.maxHealth)
                    {
                        building.HandleRepair();
                        break;
                    }
                }
            }
        }
    }

    #endregion
    
    
    #endregion
    
    #region Methods
    public Item FindItemAround()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1f);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Item>(out Item item))
            {
                if(item.TryJoin(this) || item.assignBuilder == this)
                    return item;
            }
        }
        return null;
    }
    
    public void PickupItem(Item item)
    {
        int addedAmount = currentInventory.Add(item.resourceType, item.amount);

        if (addedAmount > 0)
        {
            if (addedAmount >= item.amount)
            {
                PoolManager.Instance.Despawn(item.gameObject);
            }
            else
            {
                item.amount -= addedAmount;
            }
            Debug.Log($"Collected Item: {item.resourceType}, Amount:  {item.amount}");
        }
    }
    
    public bool FindAvailableTask()
    {
        var task = TaskManager.Instance
            .GetAvailableTasks()
            .FirstOrDefault();

        if (task == null)
            return false;

        currentTask = task;
        return true;
    }
    
    public bool CheckPathToObstacleObject()
    {
        if (builderBlackBoard.currentObstacle == null)
            return false;

        var obstacleGo = builderBlackBoard.currentObstacle as DecorObject;
        if (obstacleGo == null)
            return false;

        var path = FindBestPathToAnyAdjacent(
            obstacleGo.gameObject,
            obstacleGo.layerIndex
        );

        if (path == null)
            return false;

        builderBlackBoard.pathFinding = path;
        return true;
    }

    public GameObject FindInterestObject()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            3f
        );

        List<GameObject> validTargets = new List<GameObject>();

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject)
                continue;

            if (hit.CompareTag("Animal") || hit.CompareTag("Building"))
            {
                validTargets.Add(hit.gameObject);
            }
        }

        if (validTargets.Count == 0)
            return null;

        return validTargets[Random.Range(0, validTargets.Count)];
    }

    public void ResetState()
    {
        builderBlackBoard.currentObstacle = null;
        builderBlackBoard.pathFinding = null;
        currentResource = ResourceType.None;
        currentTool = ToolType.None;
        currentTask = null;
        currentState = UnitState.Idle;
        animState = AnimState.Idle;
        targetGO = null;
        UpdateAnim();
        animFSM.ChangeState(UnitState.Idle, AnimState.Idle);
    }
    
    public void UpdateAnim()
    {
        animFSM.SetResource(currentResource);
        animFSM.SetTool(currentTool);
    }
    
    public Building FindBestBuildingToRepair()
    {
        List<Building> destroyedBuildings = new List<Building>();
        List<Building> damagedBuildings = new List<Building>();

        foreach (var b in UnitManager.Instance.buildings)
        {
            if (b == null) continue;

            if (b.buildingState == BuildingState.Destroyed)
            {
                destroyedBuildings.Add(b);
            }
            else
            {
                var healthComp = b.GetComponentInChildren<Health>();
                if (healthComp != null && healthComp.CurrentHealth < healthComp.maxHealth)
                {
                    damagedBuildings.Add(b);
                }
            }
        }

        Building FindNearestByPath(List<Building> list)
        {
            if (list.Count == 0) return null;

            // TỐI ƯU HÓA: Chỉ lấy 5 nhà gần nhất theo khoảng cách vật lý (đường chim bay)
            // để chạy thuật toán tìm đường, tránh làm tụt FPS game.
            var closestCandidates = list
                .OrderBy(b => (b.transform.position - transform.position).sqrMagnitude)
                .Take(5);

            Building nearestBuilding = null;
            float minCost = float.MaxValue;

            // Chạy A* để tìm xem trong 5 nhà này, nhà nào đi tới tốn ít bước nhất
            foreach (var candidate in closestCandidates)
            {
                var path = FindBestPathToAnyAdjacent(candidate.gameObject, candidate.layerIndex);
                
                if (path != null && path.totalCost < minCost)
                {
                    minCost = path.totalCost;
                    nearestBuilding = candidate;
                }
            }

            return nearestBuilding;
        }

        // 3. XỬ LÝ ƯU TIÊN
        
        // Ưu tiên 1: Quét danh sách nhà bị Sập (Destroyed)
        Building target = FindNearestByPath(destroyedBuildings);

        // Ưu tiên 2: Nếu không có nhà sập (hoặc nhà sập bị bịt kín không có đường vào)
        // thì chuyển sang tìm nhà bị xước máu (Damaged)
        if (target == null)
        {
            target = FindNearestByPath(damagedBuildings);
        }

        return target; // Trả về null nếu toàn bộ nhà đều đầy máu
    }
    #endregion
    
    #region Move to task target
    public bool IsCollidingWithTaskTarget()
    {
        if (currentTask == null || currentTask.targetGameObject == null)
            return false;

        var pawnCol = GetComponent<CircleCollider2D>();
        var targetCol = currentTask.targetGameObject.GetComponent<Collider2D>();

        if (pawnCol == null || targetCol == null)
            return false;

        ColliderDistance2D dist = pawnCol.Distance(targetCol);

        return dist.distance <= 0.05f; 
    }
    
    
    #endregion

    #region  Instaniate Object
    public GameObject InstaniateObject(GameObject obj, Vector3 worldPosition, int currentLayerIndex, int amount)
    {
        GameObject parentTransform = GameObject.Find("ItemSpawned");
        var spawnedObj = PoolManager.Instance.Spawn(obj, 
            worldPosition, Quaternion.identity);
        
        

        if (worldPosition.x > transform.position.x)
            spawnedObj.transform.localScale = new Vector3(-1, 1, 1);
        
        var itemComponent = spawnedObj.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.layerIndex = currentLayerIndex;
            itemComponent.amount = amount;
        }
        
        if (spawnedObj != null)
        {
            itemComponent.StartDrop(worldPosition, transform.position);
        }
        return spawnedObj;
    }
    
    #endregion

    
    public override void UseSpecialAbility()
    {
        throw new System.NotImplementedException();
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Màu cho workRange
        Gizmos.color = Color.yellow;

        // Xác định hướng nhìn (giống TryChop)
        float facing = transform.localScale.x >= 0 ? 1f : -1f;
        Vector2 facingDir = new Vector2(facing, 0f);

        // Điểm origin của OverlapBox
        Vector2 origin = (Vector2)transform.position + facingDir * workRange;

        // Vẽ đường biểu diễn workRange
        Gizmos.DrawLine(transform.position, origin);

        // Màu cho workBox
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);

        // Vẽ box (wire + solid để dễ nhìn)
        Gizmos.DrawWireCube(origin, workBoxSize);
        Gizmos.DrawCube(origin, workBoxSize);
    }
#endif

}
