using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.Node.BuilderNode;
using _Script.BT.Node.BuilderNode.Build;
using _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence;
using _Script.Task;
using _Script.Unit_Management_System.Animation;
using UnityEngine;

public class Builder : Unit
{
    private BehaviourTree bt;
    
    [Header("Builder Info")]
    public Vector2 workBoxSize = new Vector2(1f, 1f);
    public float workRange = 1f;
    public ToolType currentTool = ToolType.None;
    public ResourceType currentResource = ResourceType.None;
    
    [Header("Animation FSM")]
    public AnimationFSM animFSM;
    
    public BuilderBlackBoard builderBlackBoard { get; private set; }
    public GameObject targetGO;
    
    private IChoppable currentTarget;

    protected override void Awake()
    {
        base.Awake();
        bt = CreateBuilderBT(this);
        animFSM = GetComponent<AnimationFSM>();
        builderBlackBoard = new BuilderBlackBoard();
    }
    
    private void Update()
    {
        bt?.Tick();
    }
    
    #region BT
    public BehaviourTree CreateBuilderBT(Builder builder)
    {
        var chopTreeSequence = new SequenceNode(
            new HasChopTaskNode(builder),
            new FindChopTaskNode(builder),
            new AssignTaskNode(builder),
            new CheckPathToTargetNode(builder),
            new MoveToTargetNode(builder),
            new ChopNode(builder)
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
            new HasBuildTaskNode(builder),
            new FindBuildTaskNode(builder),
            new AssignTaskNode(builder),
            new CheckPathToTargetNode(builder),
            clearObstacleLoop,
            new MoveToTargetNode(builder),
            new BuildNode(builder)
        );

        var root = new SelectorNode(
            chopTreeSequence,
            buildStructureSequence,
            new IdleNode(builder)
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

        if (currentTask.IsCompleted)
        {
            currentTask = null;
            return true;
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
                return true;
            }
        }

        if (target is DecorObject)
        {
            var obj = target as DecorObject;
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
    
    public void TryBuild()
    {
        var facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        var origin = (Vector2)transform.position + facingDir * workRange;

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, workBoxSize, 0f);
        
        foreach (var hit in hits)
        {
            if (hit?.gameObject == null)
                continue;

            if (hit.CompareTag("Building") && hit.gameObject == currentTask.targetGameObject)
            {
                var building = hit.gameObject.GetComponent<Building>();
                building.HandleBuilt();
                break;
            }
        }
    }

    #endregion
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

    public bool AssignCurrentTask()
    {
        if (currentTask == null)
            return false;

        return currentTask.TryJoin(this);
    }

    public bool CheckPathToObstacleObject()
    {
        if (builderBlackBoard.currentObstacle == null)
            return false;

        var obstacleGO = (builderBlackBoard.currentObstacle as Component)?.gameObject;
        if (obstacleGO == null)
            return false;

        var path = FindBestPathToAnyAdjacent(
            obstacleGO,
            characterMovement.CurrentLayer
        );

        if (path == null)
            return false;

        builderBlackBoard.pathFinding = path;
        return true;
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

    public void MoveHorizontallyToTarget(GameObject target)
    {
        if (target.Equals(null))
            return;

        var rb = characterMovement.rb;

        Vector2 myPos = rb.position;
        Vector2 targetPos = target.transform.position;

        float dx = targetPos.x - myPos.x;
        
        if (Mathf.Abs(dx) <= 0.05f)
            return;

        float dir = Mathf.Sign(dx);

        characterMovement.HandleFlipByPosition(targetPos);

        Vector2 nextPos = new Vector2(
            myPos.x + dir * characterMovement.moveSpeed * Time.fixedDeltaTime,
            myPos.y
        );

        rb.MovePosition(nextPos);
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
