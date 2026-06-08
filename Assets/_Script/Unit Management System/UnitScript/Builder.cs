using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.GlobalAlarm;
using _Script.BT.Node.BuilderNode;
using _Script.BT.Node.BuilderNode.Build;
using _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence;
using _Script.BT.Node.BuilderNode.Idle;
using _Script.BT.Node.BuilderNode.RepairStructure;
using _Script.Enum;
using _Script.ItemScript;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using _Script.Task;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;
using Random = UnityEngine.Random;

public class Builder : Unit
{
    [Header("Builder Info")] public Vector2 workBoxSize = new(1f, 1f);
    public float workRange = 1f;

    public bool isPanicking { get; set; }

    [Header("Task")] public Task currentTask;

    [Header("Carry Item")] public ToolType currentTool = ToolType.None;

    public ResourceType currentResource = ResourceType.None;

    [Header("Tartget Game Object")] public GameObject targetGO;

    [Header("Inventory")] public UnitInventory currentInventory;

    private IChoppable currentTarget;

    public float CurrentWorkRate
    {
        get
        {
            if (unitStatsManager.GetBaseData() is BuilderStatsSO builderData)
            {
                var levelMultiplier = unitStatsManager.currentLevel - 1;
                return builderData.baseWorkRate + builderData.workRatePerLevel * levelMultiplier;
            }

            Debug.LogError($"[Builder] Quên gắn file BuilderStatsSO cho {gameObject.name}!");
            return 0f;
        }
    }

    public bool IsBusy => currentTask != null && !currentTask.IsCompleted && currentTask.targetGameObject != null;

    public BuilderBlackBoard builderBlackBoard { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        bt = CreateBuilderBT(this);
        builderBlackBoard = new BuilderBlackBoard();
        currentInventory = GetComponentInChildren<UnitInventory>();
    }

    protected override void Update()
    {
        if (Mathf.Approximately(Time.timeScale, 0f))
            return;

        base.Update();

        if (!isPanicking)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, viewDistance);
            foreach (var hit in hits)
                if (hit != null && hit.CompareTag("Enemy"))
                {
                    Debug.LogWarning(
                        $"[Sensor Update] 🚨 {gameObject.name} phát hiện quái vật {hit.name}! Bật cờ hoảng loạn!");

                    isPanicking = true;
                    characterMovement.RequestStopMoving();
                    bt?.ClearState();
                    var enemyLayer = hit.GetComponentInChildren<FloorAgent>()._currentFloorIndex;

                    GlobalAlarmSystem.TriggerAlarm(hit.gameObject, hit.transform.position, enemyLayer);
                    break;
                }
        }

        bt?.Tick();
        animFSM.ChangeState(currentState, animState);
    }

    #region BT

    public BehaviourTree CreateBuilderBT(Builder builder)
    {
        var itemDropWaitTime = 0.8f;
        var waitAfterWork = new WaitDurationNode(builder, itemDropWaitTime);

        var isCollidingWithTarget = new IsCollidingWithTaskTargetNode(builder);

        // ==========================================
        // 1. CHOP TREE BRANCH (Nhánh chặt cây)
        // ==========================================
        var chopImmediately = new SequenceNode(
            isCollidingWithTarget,
            new ChopNode(builder),
            waitAfterWork
        );

        var walkAndChop = new SequenceNode(
            new CheckPathToAdjacentTargetNode(builder),
            new MoveToTargetNode(builder),
            new ChopNode(builder),
            waitAfterWork
        );

        var continueChopSequence = new SequenceNode(
            new HasCurrentTaskOfTypeNode(builder, TaskType.ChopTree),
            new SelectorNode(chopImmediately, walkAndChop)
        );

        var findNewChopSequence = new SequenceNode(
            new IsIdleNode(builder),
            new HasChopTaskNode(builder),
            new FindChopTaskNode(builder),
            new AssignTaskNode(builder),
            new CheckPathToAdjacentTargetNode(builder),
            new MoveToTargetNode(builder),
            new ChopNode(builder),
            waitAfterWork
        );

        var chopTreeSelector = new SelectorNode(continueChopSequence, findNewChopSequence);


        // ==========================================
        // 2. BUILD STRUCTURE BRANCH (Nhánh xây nhà)
        // ==========================================
        var clearObstacleLoop = new RepeatUntilFailureNode(
            new SequenceNode(
                new HasObstacleNode(builder),
                new FindPathToObstacleNode(builder),
                new MoveToObstacleNode(builder),
                new ChopNode(builder),
                waitAfterWork
            )
        );

        var buildImmediately = new SequenceNode(
            isCollidingWithTarget,
            new BuildNode(builder),
            waitAfterWork
        );

        var walkAndBuild = new SequenceNode(
            new CheckPathToAdjacentTargetNode(builder),
            clearObstacleLoop,
            new MoveToTargetNode(builder),
            new BuildNode(builder),
            waitAfterWork
        );

        var continueBuildSequence = new SequenceNode(
            new HasCurrentTaskOfTypeNode(builder, TaskType.BuildStructure),
            new SelectorNode(buildImmediately, walkAndBuild)
        );

        var findNewBuildSequence = new SequenceNode(
            new IsIdleNode(builder),
            new HasBuildTaskNode(builder),
            new FindBuildTaskNode(builder),
            new AssignTaskNode(builder),
            new CheckPathToAdjacentTargetNode(builder),
            clearObstacleLoop,
            new MoveToTargetNode(builder),
            new BuildNode(builder),
            waitAfterWork
        );

        var buildStructureSelector = new SelectorNode(continueBuildSequence, findNewBuildSequence);


        // ==========================================
        // 3. REPAIR STRUCTURE BRANCH (Nhánh sửa nhà)
        // ==========================================
        var repairImmediately = new SequenceNode(
            isCollidingWithTarget,
            new RepairNode(builder),
            waitAfterWork
        );

        var walkAndRepair = new SequenceNode(
            new CheckPathToAdjacentTargetNode(builder),
            new MoveToTargetNode(builder),
            new RepairNode(builder),
            waitAfterWork
        );

        var continueRepairSequence = new SequenceNode(
            new HasCurrentTaskOfTypeNode(builder, TaskType.RepairStructure),
            new SelectorNode(repairImmediately, walkAndRepair)
        );

        var findNewRepairSequence = new SequenceNode(
            new IsDawnNode(builder),
            new IsIdleNode(builder),
            new HasRepairBuildingTaskNode(builder),
            new FindRepairTaskNode(builder),
            new AssignTaskNode(builder),
            new CheckPathToAdjacentTargetNode(builder),
            new MoveToTargetNode(builder),
            new RepairNode(builder),
            waitAfterWork
        );

        var repairStructureSelector = new SelectorNode(continueRepairSequence, findNewRepairSequence);

        // ==========================================
        // 4. CÁC NHÁNH KHÁC & ROOT SELECTOR (Giữ nguyên)
        // ==========================================
        // ... Phần dưới giữ nguyên hoàn toàn như cũ ...
        var emergencyTransportSequence = new SequenceNode(
            new HasItemInInventoryNode(builder),
            new CreateTransportTask(builder),
            new AssignTaskNode(builder),
            new CheckPathToFrontTargetNode(builder),
            new MoveToTargetNode(builder),
            new TransportItemNode(builder)
        );

        var transportItemSequence = new SequenceNode(
            new IsDawnNode(builder),
            new HasItemInInventoryNode(builder),
            new CreateTransportTask(builder),
            new AssignTaskNode(builder),
            new CheckPathToFrontTargetNode(builder),
            new MoveToTargetNode(builder),
            new TransportItemNode(builder)
        );

        var collectWorldItemSequence = new SequenceNode(
            new IsIdleNode(builder),
            new HasEmptySpaceNode(builder),
            new MoveToWorldItemActionNode(builder)
        );

        var wanderFreeSequence = new SequenceNode(
            new HasIdleTimeNode(builder),
            new MoveFollowAvaiablePathNode(builder),
            new WaitRandomTimeNode(builder)
        );

        var idleSelector = new SelectorNode(
            collectWorldItemSequence,
            wanderFreeSequence
        );

        var root = new SelectorNode(
            new PanicFleeActionNode(builder),
            new SequenceNode(
                new IsInventoryFullNode(builder),
                emergencyTransportSequence
            ),
            repairStructureSelector,
            new SelectorNode(
                buildStructureSelector,
                chopTreeSelector
            ),
            transportItemSequence,
            idleSelector
        );

        return new BehaviourTree(root);
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

        var dist = pawnCol.Distance(targetCol);

        return dist.distance <= 0.05f;
    }

    #endregion

    #region Instaniate Object

    public GameObject InstaniateObject(GameObject obj, Vector3 worldPosition, int currentLayerIndex, int amount)
    {
        var parentTransform = GameObject.Find("ItemSpawned");
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

        itemComponent.assignBuilder = this;
        if (spawnedObj != null) itemComponent.StartDrop(worldPosition, transform.position);

        return spawnedObj;
    }

    #endregion


    public override void UseSpecialAbility()
    {
        throw new NotImplementedException();
    }

    public override List<(string name, string value)> GetSpecialStats()
    {
        var extraStats = new List<(string name, string value)>();

        extraStats.Add(("Work Rate", CurrentWorkRate.ToString(CultureInfo.InvariantCulture)));

        return extraStats;
    }

    #region Do Task

    public bool IsChopped()
    {
        if (targetGO == null)
            return true;

        var targetPos = targetGO.transform.position;
        var dir = targetPos - transform.position;

        if (dir.x != 0)
        {
            var scale = transform.localScale;
            scale.x = dir.x < 0
                ? -Mathf.Abs(scale.x)
                : Mathf.Abs(scale.x);

            transform.localScale = scale;
        }

        if (targetGO.TryGetComponent<Tree>(out var tree))
        {
            if (tree.currentChopHit >= tree.maxChopHit)
            {
                if (currentTask != null)
                {
                    currentTask.taskStatus = TaskStatus.Completed;
                    TaskManager.Instance.RemoveTask(currentTask);
                    currentTask = null;
                }

                InstaniateObject(PrefabConfig.Instance.woodPrefab,
                    tree.gameObject.transform.position, tree.layerIndex, 1);

                var coinObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.coinPrefab, transform.position,
                    Quaternion.identity);
                coinObj.GetComponent<Coin>().StartDrop(coinObj.transform.position, layerIndex);

                return true;
            }
        }
        else
        {
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
        }

        return false;
    }

    public void TryChop()
    {
        var facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        var origin = (Vector2)transform.position + facingDir * workRange;

        var hits = Physics2D.OverlapBoxAll(origin, workBoxSize, 0f);

        foreach (var hit in hits)
        {
            if (hit?.gameObject == null)
                continue;

            if (hit.CompareTag("Tree") && hit.gameObject == currentTask.targetGameObject)
            {
                currentTarget = hit.gameObject.GetComponent<Tree>();
                PlayAxeHitSFX();
                break;
            }

            if (hit.CompareTag("Bush") &&
                hit.gameObject.GetComponent<IChoppable>() == builderBlackBoard.currentObstacle)
            {
                currentTarget = hit.gameObject.GetComponent<Bush>();
                PlayAxeHitSFX();
                break;
            }

            if (hit.CompareTag("Rock") &&
                hit.gameObject.GetComponent<IChoppable>() == builderBlackBoard.currentObstacle)
            {
                currentTarget = hit.gameObject.GetComponent<Rock>();
                PlayAxeHitSFX();
                break;
            }
        }

        if (currentTarget != null) currentTarget.HandleChopped();
    }

    public bool IsCompletedBuild()
    {
        if (currentTask == null || currentTask.targetGameObject == null)
            return true;

        var targetPos = currentTask.targetGameObject.transform.position;
        var dir = targetPos - transform.position;

        if (dir.x != 0)
        {
            var scale = transform.localScale;
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

        var targetPos = currentTask.targetGameObject.transform.position;
        var dir = targetPos - transform.position;

        if (dir.x != 0)
        {
            var scale = transform.localScale;
            scale.x = dir.x < 0
                ? -Mathf.Abs(scale.x)
                : Mathf.Abs(scale.x);

            transform.localScale = scale;
        }

        var building = currentTask.targetGameObject.GetComponent<Building>();

        if (building.buildingState == BuildingState.Completed
            && building.health.IsFull())
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

        var hits = Physics2D.OverlapBoxAll(origin, workBoxSize, 0f);

        foreach (var hit in hits)
        {
            if (hit?.gameObject == null)
                continue;

            if (hit.gameObject != currentTask.targetGameObject)
                continue;

            var building = hit.gameObject.GetComponent<Building>();

            if (currentTask.taskType == TaskType.BuildStructure)
                if (building != null)
                {
                    building.HandleBuilt(CurrentWorkRate);
                    PlayHammerHitSFX();
                    break;
                }

            if (currentTask.taskType == TaskType.RepairStructure)
            {
                var health = hit.gameObject.GetComponentInChildren<Health>();

                if (health != null && building != null)
                    if (health.CurrentHealth < health.maxHealth)
                    {
                        building.HandleRepair();
                        PlayHammerHitSFX();
                        break;
                    }
            }
        }
    }

    #endregion

    #region Methods


    public void PickupItem(Item item)
    {
        if (item == null || item.itemData == null) return;

        var addedAmount = currentInventory.Add(item.itemData, item.amount);

        if (addedAmount > 0)
        {
            if (addedAmount >= item.amount)
            {
                if (ItemManager.Instance != null) ItemManager.Instance.UnregisterItem(item);

                PoolManager.Instance.Despawn(item.gameObject);
            }
            else
            {
                item.amount -= addedAmount;
            }

            Debug.Log(
                $"Collected Item: {item.itemData.itemName}, Amount: {addedAmount}, Remaining in world: {item.amount}");
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
        var hits = Physics2D.OverlapCircleAll(
            transform.position,
            3f
        );

        var validTargets = new List<GameObject>();

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            if (hit.CompareTag("Animal") || hit.CompareTag("Building")) validTargets.Add(hit.gameObject);
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

        isPanicking = false;
    }

    public void UpdateAnim()
    {
        animFSM.SetResource(currentResource);
        animFSM.SetTool(currentTool);
    }

    public Building FindBestBuildingToRepair()
    {
        var destroyedBuildings = new List<Building>();
        var damagedBuildings = new List<Building>();

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
                if (healthComp != null && healthComp.CurrentHealth < healthComp.maxHealth &&
                    b.buildingState == BuildingState.Completed) damagedBuildings.Add(b);
            }
        }

        Building FindNearestByPath(List<Building> list)
        {
            if (list.Count == 0) return null;

            var closestCandidates = list
                .OrderBy(b => (b.transform.position - transform.position).sqrMagnitude)
                .Take(5);

            Building nearestBuilding = null;
            var minCost = float.MaxValue;

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

        var target = FindNearestByPath(destroyedBuildings);

        if (target == null) target = FindNearestByPath(damagedBuildings);

        return target;
    }

    /// <summary>
    ///     Thuần túy tìm kiếm và trả về 1 trong 8 ô liền kề hợp lệ (Walkable) xung quanh một vị trí mục tiêu.
    ///     Ưu tiên chọn ô gần với vị trí hiện tại của Builder nhất.
    /// </summary>
    /// <param name="targetGridPos">Vị trí ô Grid của vật phẩm/mục tiêu</param>
    /// <param name="targetLayerIndex">Tầng (Layer Index) của mục tiêu</param>
    /// <returns>Tọa độ ô Grid liền kề hợp lệ, hoặc trả về chính targetGridPos nếu bị kẹt hoàn toàn</returns>
    public Vector3Int FindAdjacentWalkableCell(Vector3Int targetGridPos, int targetLayerIndex)
    {
        var originalNode = GraphNode.Instance.GetNode(targetGridPos, targetLayerIndex);
        if (originalNode != null && originalNode.isWalkable) return targetGridPos;
        var adjacentDirections = new[]
        {
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };

        var bestCell = targetGridPos;
        var minDistance = Mathf.Infinity;
        var foundValidCell = false;

        foreach (var dir in adjacentDirections)
        {
            var neighborPos = targetGridPos + dir;

            var node = GraphNode.Instance.GetNode(neighborPos, targetLayerIndex);

            if (node != null && node.isWalkable)
            {
                var distance = Vector2.Distance(transform.position, (Vector3)neighborPos);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestCell = neighborPos;
                    foundValidCell = true;
                }
            }
        }

        return bestCell;
    }

    public bool CanCalculatePathToTarget(Vector3Int targetGridPos, int targetLayerIndex)
    {
        if (GraphNode.Instance == null) return false;

        var testPath = PathfindingAlgorithm.Instance.FindMultiLayerPath(
            Vector3Int.FloorToInt(transform.position),
            layerIndex,
            targetGridPos,
            targetLayerIndex
        );

        return testPath.totalCost > 0;
    }

    #endregion

    #region Unity Lifecycle Events

    protected override void HandleGlobalAlarm(GameObject enemy, Vector3 spottedPosition, int layerIndex)
    {
        if (isPanicking) return;

        if (Vector2.Distance(transform.position, spottedPosition) > hearRange) return;

        Debug.LogWarning(
            $"[Global Alarm Event] 🚨 {gameObject.name} nhận được tín hiệu báo động! Có {enemy.name} tại {spottedPosition}! Bật cờ hoảng loạn lập tức!");

        isPanicking = true;

        if (characterMovement != null) characterMovement.RequestStopMoving();

        bt?.ClearState();
    }

    #endregion

    #region Play SFX

    public void PlayHammerHitSFX()
    {
        AudioManager.Instance.PlaySFX3D(SoundNames.SfxHammerHit, audioSource);
    }

    public void PlayAxeHitSFX()
    {
        AudioManager.Instance.PlaySFX3D(SoundNames.SfxAxeHit, audioSource);
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        var facing = transform.localScale.x >= 0 ? 1f : -1f;
        var facingDir = new Vector2(facing, 0f);

        var origin = (Vector2)transform.position + facingDir * workRange;

        Gizmos.DrawLine(transform.position, origin);

        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);

        Gizmos.DrawWireCube(origin, workBoxSize);
        Gizmos.DrawCube(origin, workBoxSize);
    }
#endif
}