using System;
using System.Collections;
using System.Collections.Generic;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.Node.LancerNode.LancerIdle;
using _Script.BT.Node.WarriorNode.WarriorCombat;
using _Script.BT.Node.WarriorNode.WarriorCombat.ReturnToBuilding;
using _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition;
using _Script.BT.Node.WarriorNode.WarriorCombat.WarriorChaseEnemy;
using _Script.BT.Node.WarriorNode.WarriorIdle;
using _Script.Unit_Management_System.Animation;
using UnityEditor;
using UnityEngine;

public class Warrior : Unit
{
    [Header("Warrior Specific")]
    public float attackRange = 1f;
    public float viewDistance = 3f;
    
    [Header("Detect Point")]
    public Transform detectPoint;

    [Range(0, 360)]
    public float viewAngle;
    
    public WarriorBlackBoard warriorBlackBoard;
    public AnimationFSM animFSM;
    
    private BehaviourTree bt;
    

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Warrior;
        animFSM = gameObject.GetComponent<AnimationFSM>();
        bt = CreateBehaviourTree(this);
        warriorBlackBoard = new WarriorBlackBoard();
    }

    private void Update()
    {
        bt?.Tick();
        CheckEnemyAggro();
		UpdateDetectPointPosition();
        UpdateSensors();
   
        if(warriorBlackBoard.detectedEnemy != null)
        {
            UpdateDirection(transform.position, 
                warriorBlackBoard.detectedEnemy.transform.position);
        }
        animFSM.ChangeState(currentState, animState);
    }


    #region  Behaviour Tree

    private BehaviourTree CreateBehaviourTree(Warrior warrior)
    {
        var holdBorderSequence = new SequenceNode(
            new HasMaxDistanceExceeded(warrior), 
            new WarriorStopMovingNode(warrior)         
        );
        
        var attackSequence = new SequenceNode(
            new IsEnemyInAttackRangeWarriorNode(warrior),
            new WarriorAttackNode(warrior));

        var chaseSequence = new SequenceNode(
            new HasEnemyInWarriorSight(warrior),
            new IsEnemyOutOfWarriorAttackRangeNode(warrior),
            new WarriorChaseEnemy(warrior)
        );

        var searchLastSeenPositionSequence = new SequenceNode(
            new HasAggroTargetNode(warrior),
            new IsTargetVisibleNode(warrior),
            new MoveToLastSeenPositionNode(warrior),
            new ReachedLastSeenPositionNode(warrior),
            new ClearTargetNode(warrior));
        
        var combatSequence = new SequenceNode(
            new SelectorNode(
                attackSequence,
                holdBorderSequence,
                chaseSequence,
                searchLastSeenPositionSequence
                //, new WarriorDefendNode(warrior)
            )
        );

        var idleSequence = new SequenceNode(
            new WarriorHasNoEnemyInSightNode(warrior),
            new WarriorFindNextPatrolPositionNode(warrior),
            new WarriorMoveToNextPatrolPositionNode(warrior),
            new WaitNode(warrior));
        
        var combatBranch = new SequenceNode(
            new HasAggroTargetNode(warrior),
            combatSequence
        );
        
        var root = new SelectorNode(
            //death sequence,
            //hurt/stun sequence,
            combatBranch,
            idleSequence
        );
        return new BehaviourTree(root);
    }

    #endregion

    #region Method

    public List<GameObject> DetectEnemies(float range, Vector2 dir)
    {
        List<GameObject> enemiesInRange = new List<GameObject>();
        int size = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            range,
            results,
            enemyLayer);

        dir.Normalize();

        if (Camera.main == null) return enemiesInRange;
        Vector2 cameraPos = Camera.main.transform.position;

        for (var i = 0; i < size; i++)
        {
            var hit = results[i];
            if (hit == null || !hit.CompareTag("Enemy"))
                continue;

            Vector2 dirToEnemy = (hit.transform.position - transform.position).normalized;
            /*if (Vector2.Dot(dir, dirToEnemy) <= 0)
                continue;*/
            
            float angle = Vector2.Angle(dir, dirToEnemy);
            if (angle > viewAngle / 2f)
                continue;

            Bounds b = hit.bounds;

            Vector2[] samplePoints =
            {
                b.center,
                new Vector2(b.center.x, b.max.y),
                new Vector2(b.center.x, b.min.y),
                new Vector2(b.min.x, b.center.y),
                new Vector2(b.max.x, b.center.y)
            };

            bool visible = false;

            foreach (var point in samplePoints)
            {
                Vector2 dirRay = point - cameraPos;
                float dist = dirRay.magnitude;
                dirRay.Normalize();

                var ray = Physics2D.Raycast(
                    cameraPos,
                    dirRay,
                    dist,
                    obstacleLayer);

                if (ray.collider == null)
                {
                    visible = true;
                    break;
                }
            }

            if (!visible) continue;
            enemiesInRange.Add(hit.gameObject);
        }

        return enemiesInRange;
    }
    
    public bool IsEnemyInAttackRange()
    {
        if (warriorBlackBoard.detectedEnemy == null)
            return false;

        var enemy = warriorBlackBoard.detectedEnemy;

        var enemyCol = enemy.GetComponent<Collider2D>();
        if (enemyCol == null || enemyCol.isTrigger)
            return false;

        Vector2 closest = enemyCol.ClosestPoint(transform.position);

        float dist = Vector2.Distance(transform.position, closest);

        return dist <= (attackRange * 0.75);
    }

    public bool CheckEnemyStillInRange(GameObject target, float range)
    {
        int size = Physics2D.OverlapCircleNonAlloc(transform.position, range, results, enemyLayer);

        for (int i = 0; i < size; i++)
        {
            if (results[i] != null &&
                results[i].gameObject == target)
                return true;
        }

        return false;
    }
    
    private void CheckEnemyAggro()
    {
        if (warriorBlackBoard.detectedEnemy != null &&
            CheckEnemyStillInRange(warriorBlackBoard.detectedEnemy, viewDistance))
        {
            var distance = warriorBlackBoard.detectedEnemy.transform.position - transform.position;

            warriorBlackBoard.lastDirection = distance.x > 0 ? Vector2.right : Vector2.left;
            UpdateFacing(warriorBlackBoard.lastDirection);

            currentTarget = warriorBlackBoard.detectedEnemy.transform;
            lastSeenPosition = currentTarget.position;

            var flootAgent = currentTarget.GetComponentInChildren<FloorAgent>();
            lastSeenLayerIndex = flootAgent._currentFloorIndex;
            aggroTimer = aggroDuration;
            return;
        }

        if (currentTarget != null)
        {
            aggroTimer -= Time.deltaTime;

            if (aggroTimer <= 0)
            {
                //currentTarget = null;
                warriorBlackBoard.detectedEnemy = null;
            }
        }
    }
    
    public void ResetState()
    {
        currentState = UnitState.Idle;
        animState = AnimState.Idle;
        animFSM.SetWarriorDirection(WarriorDirection.None);
    }
    
    private void UpdateDetectPointPosition()
    {
        if(detectPoint == null) return;
        var target = warriorBlackBoard.detectedEnemy;
        if (target == null) return;

        Vector2 archerPos = transform.position;
        Vector2 targetPos = target.transform.position;

        Vector2 dir = (targetPos - archerPos).normalized;

        float radius = 0.4f;

        Vector2 firePos = archerPos + dir * radius;

        detectPoint.position = firePos;
    }
    
    public WarriorDirection GetDirection(Vector2 from, Vector2 to)
    {
        var dir = to - from;

        if (dir.sqrMagnitude < 0.0001f)
            return WarriorDirection.None;

        dir.Normalize();

        var dirRight = new Vector2(Mathf.Abs(dir.x), dir.y);

        var angle = Vector2.Angle(Vector2.down, dirRight);

        return angle switch
        {
            <= 45f => WarriorDirection.Down,   
            <= 135f => WarriorDirection.Front, 
            _ => WarriorDirection.Up           
        };
    }
    
    private void UpdateDirection(Vector2 from, Vector2 to)
    {
        var dir = GetDirection(from, to);
        animFSM.SetWarriorDirection(dir);
    }

    public void ClearAggro()
    {
        currentTarget = null;
        lastSeenPosition = Vector2.zero;
        lastSeenLayerIndex = -1;
    }
    
    private void UpdateSensors()
    {
        detectTimer += Time.deltaTime;
        if (detectTimer >= detectInterval)
        {
            detectTimer = 0f;

            if (warriorBlackBoard.detectedEnemy != null)
            {
                if (CheckEnemyStillInRange(warriorBlackBoard.detectedEnemy, viewDistance))
                {
                    return; 
                }
            }

            var dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            var enemies = DetectEnemies(viewDistance, dir);
        
            var newTarget = SelectClosestTarget(enemies); 
        
            if (newTarget != null)
            {
                warriorBlackBoard.detectedEnemy = newTarget; 
            }
        }
    }

    #endregion
    
    public override void UseSpecialAbility()
    {
     
    }
        
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        
        if(warriorBlackBoard == null )
            return;

        if (warriorBlackBoard.detectedEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, warriorBlackBoard.detectedEnemy.transform.position);
            
            Gizmos.DrawSphere(warriorBlackBoard.detectedEnemy.transform.position, 0.1f);
        }
        
        DrawVisionCone();
    }
    private void DrawVisionCone()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        
        Handles.color = new Color(1, 1, 0, 0.2f);
        Handles.DrawSolidArc(
            origin,
            Vector3.forward,
            Quaternion.Euler(0, 0, -viewAngle / 2) * direction,
            viewAngle,
            viewDistance
        );

        Gizmos.color = Color.yellow;

        Vector3 leftBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * direction;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * direction;

        Gizmos.DrawLine(origin, origin + leftBoundary * viewDistance);
        Gizmos.DrawLine(origin, origin + rightBoundary * viewDistance);
    }

#endif

}
