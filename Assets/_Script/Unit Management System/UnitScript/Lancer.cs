using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.GlobalAlarm;
using _Script.BT.Node.LancerNode;
using _Script.BT.Node.LancerNode.LancerDetectedEnemy.LancerCombatLoop;
using _Script.BT.Node.LancerNode.LancerIdle;
using _Script.BT.Node.LancerNode.LancerIntercept;
using _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition;
using _Script.BT.Node.WarriorNode.WarriorCombat.WarriorArlert;
using _Script.Unit_Management_System.Animation;
using _Script.Unit_Management_System.HealthComponent;
using UnityEditor;
using UnityEngine;

public class Lancer : Unit
{
    [Header("Lancer Specific")] 
    public float attackAngle = 30f;
    public int minRadius = 1;
    public int maxRadius = 2;
    public LancerDirection lancerDirection;
    
    [Header("Detect Point")]
    public Transform detectPoint;

    
    public int attackLayerMask;
    
    public LancerBlackBoard lancerBlackBoard;

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Lancer;
        lancerBlackBoard = new LancerBlackBoard();
        attackLayerMask = LayerMask.GetMask("NPC");
        bt = CreateBehaviourTree(this);
    }

    protected override void Update()
    {
        base.Update();
        bt?.Tick();
        CheckEnemyAggro();
        UpdateDetectPointPosition();
        UpdateSensors();
        
        if(lancerBlackBoard.detectedEnemy != null)
        {
            UpdateDirection(transform.position, lancerBlackBoard.detectedEnemy.transform.position);
        }
        animFSM.ChangeState(currentState, animState);
        
    }

    #region Behaviour Tree

    /*private BehaviourTree CreateBehaviourTree(Lancer lancer)
    {
        var idleSequence = new SequenceNode(
            new LancerHasNoEnemyInSight(lancer),
            new LancerFindNextPatrolPositionNode(lancer),
            new LancerMoveToNextPatrolPositionNode(lancer),
            new WaitNode(lancer));

        var attackSequence = new SequenceNode(
            new LancerIsEnemyInAttackRangeNode(lancer),
            new LancerAttackNode(lancer));
        
        var combatSequence = new SequenceNode(
            new SelectorNode(
                attackSequence,
                new LancerDefendNode(lancer)
            )
        );
        
        var root = new SelectorNode(
            new SequenceNode(
                new LancerSelectTargetNode(lancer),
                combatSequence
            ),
            idleSequence
        );
        return new BehaviourTree(root);
    }*/
    
    private BehaviourTree CreateBehaviourTree(Lancer lancer)
    {
        // Nhánh 1: Tấn công (Đâm liên tục nếu quái vào tầm)
        var attackSequence = new SequenceNode(
            new LancerIsEnemyInAttackRangeNode(lancer),
            new LancerStopMovingNode(lancer), 
            new IsDefendStateNode(lancer),
            new LancerAttackNode(lancer)      
        );
        
        var alarmResponseSequence = new SequenceNode(
            new IsUnitAlertedNode(lancer),
            new LancerMoveToInterceptPositionNode(lancer), 
            new ClearAlertNode(lancer)
        );

        var interceptSequence = new SequenceNode(
            new LancerHasEnemyInSightNode(lancer),
            new IsEnemyOutOfLancerAttackRangeNode(lancer),
            new LancerMoveToInterceptPositionNode(lancer)
        );

        var combatBranch = new SequenceNode(
            new HasAggroTargetNode(lancer),
            new SelectorNode(
                attackSequence,     
                interceptSequence   
            )
        );

        var patrolSequence = new SequenceNode(
            new LancerHasNoEnemyInSight(lancer),
            new LancerFindNextPatrolPositionNode(lancer), 
            new LancerMoveToNextPatrolPositionNode(lancer),
            new WaitNode(lancer)
        );

        var root = new SelectorNode(
            combatBranch, 
            alarmResponseSequence,
            patrolSequence  
        );

        return new BehaviourTree(root);
    }
    

    #endregion

    public override void UseSpecialAbility()
    {
        
    }

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
        if (lancerBlackBoard.detectedEnemy != null &&
            CheckEnemyStillInRange(lancerBlackBoard.detectedEnemy, viewDistance))
        {
            var distance = lancerBlackBoard.detectedEnemy.transform.position - transform.position;

            lancerBlackBoard.lastDirection = distance.x > 0 ? Vector2.right : Vector2.left;
            UpdateFacing(lancerBlackBoard.lastDirection);

            currentTarget = lancerBlackBoard.detectedEnemy.transform;
            lastSeenPosition = currentTarget.position;

            aggroTimer = aggroDuration;
            return;
        }

        if (currentTarget != null)
        {
            aggroTimer -= Time.deltaTime;

            if (aggroTimer <= 0)
            {
                ClearAggro();
                isAlerted = false;
                lancerBlackBoard.detectedEnemy = null;
            }
        }
    }
    
    public LancerDirection GetDirection(Vector2 from, Vector2 to)
    {
        var dir = to - from;

        if (dir.sqrMagnitude < 0.0001f)
            return LancerDirection.None;

        dir.Normalize();

        var dirRight = new Vector2(Mathf.Abs(dir.x), dir.y);

        var angle = Vector2.Angle(Vector2.down, dirRight);

        return angle switch
        {
            <= 25f => LancerDirection.Down,        
            <= 75f => LancerDirection.DownRight,   
            <= 105f => LancerDirection.Right,      
            <= 165f => LancerDirection.UpRight,   
            _ => LancerDirection.Up                
        };
    }

    private void UpdateDirection(Vector2 from, Vector2 to)
    {
        var dir = GetDirection(from, to);
        lancerDirection = dir;
        animFSM.SetLancerDirection(dir);
    }
    
    private void UpdateDetectPointPosition()
    {
        if(detectPoint == null) return;
        var target = lancerBlackBoard.detectedEnemy;
        if (target == null) return;

        Vector2 archerPos = transform.position;
        Vector2 targetPos = target.transform.position;

        Vector2 dir = (targetPos - archerPos).normalized;

        float radius = 0.4f;

        Vector2 firePos = archerPos + dir * radius;

        detectPoint.position = firePos;
    }

    public void ResetState()
    {
        currentState = UnitState.Idle;
        animState = AnimState.Idle;
        animFSM.SetLancerDirection(LancerDirection.None);
    }
    public void ResetStateWithDelay(float delay = 5f)
    {
        StartCoroutine(ResetStateAfterDelay(delay));
    }

    private IEnumerator ResetStateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetState();
    }

    public bool IsEnemyInAttackRange()
    {
        if (lancerBlackBoard.detectedEnemy == null)
            return false;

        var enemy = lancerBlackBoard.detectedEnemy;

        var enemyCol = enemy.GetComponent<Collider2D>();
        if (enemyCol == null || enemyCol.isTrigger)
            return false;

        Vector2 closest = enemyCol.ClosestPoint(transform.position);

        float dist = Vector2.Distance(transform.position, closest);

        return dist <= attackRange * 0.75;
    }
    
    private void UpdateSensors()
    {
        detectTimer += Time.deltaTime;
        if (detectTimer >= detectInterval)
        {
            detectTimer = 0f;
            var enemyLayer = -1;

            if (lancerBlackBoard.detectedEnemy != null)
            {
                enemyLayer = lancerBlackBoard.detectedEnemy.GetComponentInChildren<FloorAgent>()._currentFloorIndex;
                if (CheckEnemyStillInRange(lancerBlackBoard.detectedEnemy, viewDistance))
                {
                    GlobalAlarmSystem.TriggerAlarm(lancerBlackBoard.detectedEnemy,
                        lancerBlackBoard.detectedEnemy.transform.position, enemyLayer);
                    return; 
                }
            }

            var dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            var enemies = DetectEnemies(viewDistance, dir);
        
            var newTarget = SelectClosestTarget(enemies); 
        
            if (newTarget != null)
            {
                lancerBlackBoard.detectedEnemy = newTarget;
                enemyLayer = lancerBlackBoard.detectedEnemy.GetComponentInChildren<FloorAgent>()._currentFloorIndex;
                GlobalAlarmSystem.TriggerAlarm(newTarget, newTarget.transform.position, enemyLayer);
            }
        }
    }

    public override UnitState GetState()
    {
        if(lancerBlackBoard.detectedEnemy != null || currentTarget != null)
            return UnitState.Defend;
        return UnitState.Idle;
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

            if (angle <= (attackAngle / 2f))
            {
                targetHealth.TakeDamage(attackDamage);
                results[i] = null; 
            }
        }
    }

    private Vector2 GetCurrentFacingVector()
    {
        Vector2 facingDir = Vector2.right; 

        switch (lancerDirection)
        {
            case LancerDirection.Up:
                facingDir = Vector2.up;
                break;
            case LancerDirection.Down:
                facingDir = Vector2.down;
                break;
            case LancerDirection.Right:
                facingDir = Vector2.right;
                break;
            case LancerDirection.UpRight:
                facingDir = new Vector2(1, 1).normalized; 
                break;
            case LancerDirection.DownRight:
                facingDir = new Vector2(1, -1).normalized; 
                break;
        }

        if (transform.localScale.x < 0 && 
            lancerDirection != LancerDirection.Up && 
            lancerDirection != LancerDirection.Down)
        {
            facingDir.x *= -1; 
        }

        return facingDir;
    }

    public override List<(string name, string value)> GetSpecialStats()
    {
        var extraStats = new List<(string name, string value)>();
        
        extraStats.Add(("Attack Damage", attackDamage.ToString(CultureInfo.InvariantCulture))); 
        extraStats.Add(("Attack CD", attackCooldown.ToString(CultureInfo.InvariantCulture)));
        
        return extraStats;
    }
    #endregion
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        
        if(lancerBlackBoard == null )
            return;

        if (lancerBlackBoard.detectedEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, lancerBlackBoard.detectedEnemy.transform.position);
            
            Gizmos.DrawSphere(lancerBlackBoard.detectedEnemy.transform.position, 0.1f);
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
        
        Vector3 direction = GetCurrentFacingVector();

        Handles.color = new Color(1f, 0f, 0f, 0.2f); 
        Handles.DrawSolidArc(
            origin,
            Vector3.forward, 
            Quaternion.Euler(0, 0, -attackAngle / 2) * direction,
            attackAngle,
            damageRange
        );

        Gizmos.color = Color.red;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, -attackAngle / 2) * direction;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, attackAngle / 2) * direction;

        Gizmos.DrawLine(origin, origin + leftBoundary * damageRange);
        Gizmos.DrawLine(origin, origin + rightBoundary * damageRange);
    }

#endif
}
