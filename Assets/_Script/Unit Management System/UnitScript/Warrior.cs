using System.Collections.Generic;
using System.Globalization;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.GlobalAlarm;
using _Script.BT.Node.LancerNode.LancerIdle;
using _Script.BT.Node.unitNode.unitCombat.SearchLastSeenPosition;
using _Script.BT.Node.WarriorNode.WarriorCombat;
using _Script.BT.Node.WarriorNode.WarriorCombat.ReturnToBuilding;
using _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition;
using _Script.BT.Node.WarriorNode.WarriorCombat.WarriorArlert;
using _Script.BT.Node.WarriorNode.WarriorCombat.WarriorChaseEnemy;
using _Script.BT.Node.WarriorNode.WarriorIdle;
using _Script.Unit_Management_System.Animation;
using _Script.Unit_Management_System.HealthComponent;
using UnityEditor;
using UnityEngine;

public class Warrior : Unit
{
    [Header("Detect Point")]
    public Transform detectPoint;
    
    public WarriorDirection warriorDirection;
    public WarriorBlackBoard warriorBlackBoard;
    public int attackLayerMask;
    
    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Warrior;
        bt = CreateBehaviourTree(this);
        warriorBlackBoard = new WarriorBlackBoard();
        attackLayerMask = LayerMask.GetMask("NPC");
    }

    protected override void Update()
    {
        base.Update();
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
            new StopMovingNode(warrior)         
        );
        
        var attackSequence = new SequenceNode(
            new IsEnemyInAttackRangeWarriorNode(warrior),
            new WarriorAttackNode(warrior));
        
        var alarmResponseSequence = new SequenceNode(
            new IsUnitAlertedNode(warrior),
            new MoveToLastSeenPositionNode(warrior), 
            new ClearAlertNode(warrior)
        );

        var responseBranchWithGuard = new SelectorNode(
            holdBorderSequence,      
            alarmResponseSequence    
        );

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
                //holdBorderSequence,
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
            responseBranchWithGuard,
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

        Vector2 myPos = transform.position;

        for (var i = 0; i < size; i++)
        {
            var hit = results[i];
            if (hit == null || !hit.CompareTag("Enemy"))
                continue;

            Vector2 dirToEnemy = (hit.transform.position - (Vector3)myPos).normalized;
            if (Vector2.Dot(dir, dirToEnemy) <= 0)
                continue;

            Bounds b = hit.bounds;
            Vector2[] samplePoints =
            {
                b.center,
                new (b.center.x, b.max.y),
                new (b.center.x, b.min.y),
                new (b.min.x, b.center.y),
                new (b.max.x, b.center.y)
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
                enemiesInRange.Add(hit.gameObject);
            }
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
                if(isAlerted)
                    ClearAggro();
                
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
        warriorDirection = dir;
        animFSM.SetWarriorDirection(dir);
    }
    
    private void UpdateSensors()
    {
        detectTimer += Time.deltaTime;
        if (detectTimer >= detectInterval)
        {
            detectTimer = 0f;
            var enemyLayer = -1;

            if (warriorBlackBoard.detectedEnemy != null)
            {
                if (CheckEnemyStillInRange(warriorBlackBoard.detectedEnemy, viewDistance))
                {
                    enemyLayer = warriorBlackBoard.detectedEnemy.GetComponentInChildren<FloorAgent>()
                        ._currentFloorIndex;
                    GlobalAlarmSystem.TriggerAlarm(warriorBlackBoard.detectedEnemy,
                        warriorBlackBoard.detectedEnemy.transform.position, enemyLayer);
                    return; 
                }
            }

            var dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            var enemies = DetectEnemies(viewDistance, dir);
        
            var newTarget = SelectClosestTarget(enemies); 
        
            if (newTarget != null)
            {
                warriorBlackBoard.detectedEnemy = newTarget;
                enemyLayer = warriorBlackBoard.detectedEnemy.GetComponentInChildren<FloorAgent>()._currentFloorIndex;
                GlobalAlarmSystem.TriggerAlarm(newTarget, newTarget.transform.position, enemyLayer);
            }
        }
    }
    
    public void DealDamage()
    {
        float damageRadius = attackRange;
        Vector2 facingDir = GetCurrentFacingVector();

        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, damageRadius, results, attackLayerMask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCol = results[i];

            if (hitCol.gameObject == gameObject) continue;

            if (hitCol.isTrigger) continue;
            
            if(hitCol.gameObject.CompareTag("NPC") || 
               hitCol.gameObject.CompareTag("Player") ||
               hitCol.gameObject.CompareTag("Building")) continue;

            var targetHealth = hitCol.GetComponentInChildren<Health>();
            
            if (targetHealth == null || targetHealth.CurrentHealth <= 0) continue;

            Vector2 closest = hitCol.ClosestPoint(transform.position);
            Vector2 dirToTarget = (closest - (Vector2)transform.position).normalized;

            float angle = Vector2.Angle(facingDir, dirToTarget);

            if (angle <= (viewAngle / 2f))
            {
                targetHealth.TakeDamage(attackDamage);
                
                results[i] = null; 
            }
        }
    }

    private Vector2 GetCurrentFacingVector()
    {
        if (warriorDirection == WarriorDirection.Up) 
            return Vector2.up;
        
        if (warriorDirection == WarriorDirection.Down) 
            return Vector2.down;

        return transform.localScale.x > 0 ? Vector2.right : Vector2.left;
    }

    #endregion
    
    public override void UseSpecialAbility()
    {
     
    }
    
    public override List<(string name, string value)> GetSpecialStats()
    {
        var extraStats = new List<(string name, string value)>();
        
        extraStats.Add(("Attack Damage", attackDamage.ToString(CultureInfo.InvariantCulture))); 
        extraStats.Add(("Attack CD", attackCooldown.ToString(CultureInfo.InvariantCulture)));
        
        return extraStats;
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
        DrawAttackCone();
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
    
    private void DrawAttackCone()
    {
        Vector3 origin = transform.position;
        float damageRange = attackRange;
        
        Vector3 direction = Vector3.right;
        if (warriorDirection == WarriorDirection.Up) direction = Vector3.up;
        else if (warriorDirection == WarriorDirection.Down) direction = Vector3.down;
        else direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;

            Handles.color = new Color(1f, 0f, 0f, 0.2f); 
            Handles.DrawSolidArc(
                origin,
                Vector3.forward, 
                Quaternion.Euler(0, 0, -viewAngle / 2) * direction,
                viewAngle,
                damageRange
            );

            Gizmos.color = Color.red;

            Vector3 leftBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * direction;
            Vector3 rightBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * direction;

            Gizmos.DrawLine(origin, origin + leftBoundary * damageRange);
            Gizmos.DrawLine(origin, origin + rightBoundary * damageRange);
            
        }
#endif
}




