using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.GlobalAlarm;
using _Script.BT.Node.ArcherNode.ArcherDetectedEnemy;
using _Script.BT.Node.ArcherNode.ArcherIdle;
using _Script.BT.Node.BuilderNode.Idle;
using _Script.BT.Node.LancerNode.LancerDetectedEnemy;
using _Script.BT.Node.LancerNode.LancerIdle;
using _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition;
using _Script.ItemScript;
using _Script.Object_Pooling;
using _Script.Unit_Management_System.Animation;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Archer : Unit
{
    [Header("Archer Stat")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float fireRate = 1f;
    public float nextFireTime = 0f;
    
    [Header("Archer Specific")]
    public Transform firePoint;
    public bool isStationed = false;
    
    [Header("Vision")]
    public float viewDistance = 5f;
    [Range(0, 360)]
    public float viewAngle;

    public ArcherBlackBoard archerBlackBoard {get; set;}

    private RaycastHit2D[] raycastResults = new RaycastHit2D[10];

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Archer;

        archerBlackBoard = new ArcherBlackBoard();
    


        bt = CreateBehaviourTree(this);
    }

    protected override void Update()
    {
        base.Update();
        UpdateIsStationed();
        CheckIsStationed();
        UpdateSensors();
        UpdateFirePointPosition();
        CheckEnemyAggro();
        animFSM.ChangeState(currentState, animState);
        
        bt?.Tick();
    }

    #region BehaviourTree

    private BehaviourTree CreateBehaviourTree(Archer archer)
    {
        var idleSequence = new SequenceNode(
            new HasNoEnemyInRangeNode(archer),
            new RotateScanNode(archer),
            new WaitRandomTimeNode(archer)
        );

        // Tách riêng hành động Bắn thành một chuỗi nhỏ
        var attackActionSequence = new SequenceNode(
            new IsArcherCooldownReadyNode(archer), // Trả về Success nếu đã hồi xong, Failure nếu đang chờ
            new ShootArrowNode(archer)             // Thực hiện bắn và reset lại thời gian hồi chiêu
        );

        var detectedSequence = new SequenceNode(
            new HasAggroTargetNode(archer), // Đã bao gồm check Raycast từ Camera cực xịn của bạn
            new SelectTargetNode(archer),
            new AimAtTargetNode(archer), 
            new IsArcherInDefendStateNode(archer),
            new SelectorNode(
                attackActionSequence,         // Ưu tiên 1: Thử bắn (Nếu chưa hồi xong, node này Failure)
                new ArcherAttackCooldownNode(archer)          // Ưu tiên 2: Đứng chờ (Trả về Running, giúp BT dừng ở đây và không bị rớt xuống Idle)
            )
        );

        var root = new SelectorNode(
            detectedSequence,
            idleSequence
        );
    
        return new BehaviourTree(root);
    }

    

    #endregion
    
    public override void UseSpecialAbility()
    {
        var target = archerBlackBoard.detectedEnemy;

        if (target == null)
            return;

        Vector2 start = firePoint.transform.position;
        Vector2 targetPos = target.transform.position;

        var arrow = PoolManager.Instance.Spawn(
            PrefabConfig.Instance.arrowPrefab,
            start,
            Quaternion.identity
        );

        var arrowComponent = arrow.GetComponent<ArrowProjectile>();

        var rb = target.GetComponent<Rigidbody2D>();
        Vector2 targetVelocity = rb != null ? rb.velocity : Vector2.zero;
        
        Vector2 predictedPos = PredictTargetPosition(
            firePoint.position,
            target.transform.position,
            targetVelocity,
            arrowComponent.speed
        );

        Vector2 dir = (predictedPos - (Vector2)firePoint.position).normalized;

        arrowComponent.Init(firePoint.position, dir);

    }

    #region Method
    private void CheckIsStationed()
    {
        sortingYX.enabled = !isStationed ;
        if(isStationed)
            UpdateSortingOrderByAssignBuilding();
    }

    private void UpdateSortingOrderByAssignBuilding()
    {
        if (!isStationed || assignedBuilding == null) 
            return;
        var buildingSpriteRenderer = assignedBuilding.gameObject.GetComponent<SpriteRenderer>();
        if (buildingSpriteRenderer == null)
            return;
        
        spriteRenderer.sortingOrder = buildingSpriteRenderer.sortingOrder + 1;

    }

    private void UpdateIsStationed()
    {
        isStationed = assignedBuilding != null;
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
    
    public bool DetectEnemy(float range, Vector2 dir)
    {
        int size = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            range,
            results,
            enemyLayer);

        dir.Normalize();

        if (Camera.main == null) return false;
        Vector2 cameraPos = Camera.main.transform.position;

        for (var i = 0; i < size; i++)
        {
            var hit = results[i];
            if (hit == null || !hit.CompareTag("Enemy"))
                continue;

            Vector2 dirToEnemy = hit.transform.position - transform.position;
            dirToEnemy.Normalize();

            if (Vector2.Dot(dir, dirToEnemy) <= 0)
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
            archerBlackBoard.detectedEnemy = hit.gameObject;
            return true;
        }
        return false;
    }
    
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
            var enemyHit = results[i];
            if (enemyHit == null || !enemyHit.CompareTag("Enemy"))
                continue;

            Vector2 dirToEnemy = (enemyHit.transform.position - (Vector3)myPos).normalized;
            if (Vector2.Dot(dir, dirToEnemy) <= 0)
                continue;

            Bounds b = enemyHit.bounds;
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

                if (assignedBuilding != null)
                {
                    int hitCount = Physics2D.RaycastNonAlloc(myPos, dirRay, raycastResults, dist, obstacleLayer);
                    bool isBlockedByOther = false;

                    for (int j = 0; j < hitCount; j++)
                    {
                        GameObject hitObj = raycastResults[j].collider.gameObject;
                       
                        Transform parentTransform = hitObj.transform.parent;
                        GameObject parentObj = (parentTransform != null) ? parentTransform.gameObject : null;

                        if (parentObj != assignedBuilding.gameObject && hitObj != gameObject)
                        {
                            isBlockedByOther = true;
                            break;
                        }
                    }

                    if (!isBlockedByOther)
                    {
                        visible = true;
                        Debug.DrawLine(myPos, point, Color.green);
                        break;
                    }
                }
                else
                {
                    var ray = Physics2D.Raycast(myPos, dirRay, dist, obstacleLayer);

                    if (ray.collider == null)
                    {
                        visible = true;
                        break;
                    }
                }
            }

            if (visible)
            {
                enemiesInRange.Add(enemyHit.gameObject);
            }
        }

        return enemiesInRange;
    }
    
    public ArcherFireDirection GetFireDirection(Vector2 from, Vector2 to)
    {
        var dir = to - from;
    
        if (dir.sqrMagnitude < 0.0001f)
            return ArcherFireDirection.None;
    
        dir.Normalize();

        var angle = Vector2.Angle(Vector2.up, dir);

        return angle switch
        {
            <= 15f => ArcherFireDirection.Up,
            <= 75f => ArcherFireDirection.DiagonalUp,
            <= 105f => ArcherFireDirection.Front,
            <= 165f => ArcherFireDirection.DiagonalDown,
            _ => ArcherFireDirection.Down
        };
    }
    
    public static Vector2 PredictTargetPosition(
        Vector2 shooterPos,
        Vector2 targetPos,
        Vector2 targetVelocity,
        float projectileSpeed)
    {
        Vector2 toTarget = targetPos - shooterPos;

        float a = Vector2.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
        float b = 2f * Vector2.Dot(toTarget, targetVelocity);
        float c = Vector2.Dot(toTarget, toTarget);

        float discriminant = b * b - 4f * a * c;

        if (discriminant < 0 || Mathf.Abs(a) < 0.001f)
            return targetPos;

        float sqrt = Mathf.Sqrt(discriminant);

        float t1 = (-b - sqrt) / (2f * a);
        float t2 = (-b + sqrt) / (2f * a);

        float t = Mathf.Min(t1, t2);

        if (t < 0)
            t = Mathf.Max(t1, t2);

        if (t < 0)
            return targetPos;

        return targetPos + targetVelocity * t;
    }
    
    private void CheckEnemyAggro()
    {
        if (archerBlackBoard.detectedEnemy != null &&
            CheckEnemyStillInRange(archerBlackBoard.detectedEnemy, viewDistance))
        {
            var distance = archerBlackBoard.detectedEnemy.transform.position - transform.position;

            archerBlackBoard.lastDirection = distance.x > 0 ? Vector2.right : Vector2.left;
            UpdateFacing(archerBlackBoard.lastDirection);

            currentTarget = archerBlackBoard.detectedEnemy.transform;
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
                archerBlackBoard.detectedEnemy = null;
            }
        }
    }

    private void UpdateFirePointPosition()
    {
        if(firePoint == null) return;
        var target = archerBlackBoard.detectedEnemy;
        if (target == null) return;

        Vector2 archerPos = transform.position;
        Vector2 targetPos = target.transform.position;

        Vector2 dir = (targetPos - archerPos).normalized;

        float radius = 0.4f;

        Vector2 firePos = archerPos + dir * radius;

        firePoint.position = firePos;
    }

    public void ResetAnim()
    {
        archerBlackBoard.fireDirection =  ArcherFireDirection.None;
        animState = AnimState.Idle;
    }
    
    private void UpdateSensors()
    {
        detectTimer += Time.deltaTime;
        if (detectTimer >= detectInterval)
        {
            detectTimer = 0f;

            if (archerBlackBoard.detectedEnemy != null)
            {
                if (CheckEnemyStillInRange(archerBlackBoard.detectedEnemy, viewDistance))
                {
                    GlobalAlarmSystem.TriggerAlarm(archerBlackBoard.detectedEnemy, 
                        archerBlackBoard.detectedEnemy.transform.position);
                    return; 
                }
            }

            var dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            var enemies = DetectEnemies(viewDistance, dir);
        
            var newTarget = SelectClosestTarget(enemies); 
        
            if (newTarget != null)
            {
                GlobalAlarmSystem.TriggerAlarm(newTarget, newTarget.transform.position);
                archerBlackBoard.detectedEnemy = newTarget; 
            }
        }
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if(archerBlackBoard == null )
            return;

        if (archerBlackBoard.detectedEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, archerBlackBoard.detectedEnemy.transform.position);
            
            Gizmos.DrawSphere(archerBlackBoard.detectedEnemy.transform.position, 0.1f);
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