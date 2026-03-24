using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence;
using _Script.BT.Node.LancerNode.LancerDetectedEnemy;
using _Script.BT.Node.LancerNode.LancerDetectedEnemy.LancerCombatLoop;
using _Script.BT.Node.LancerNode.LancerIdle;
using _Script.Unit_Management_System.Animation;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lancer : Unit
{
    [Header("Lancer Specific")] 
    public float attackRange = 1f;
    public float viewDistance = 3f;
    
    [Header("Detect Point")]
    public Transform detectPoint;
    [Range(0, 360)]
    public float viewAngle;
    
    public LancerBlackBoard lancerBlackBoard;
    public AnimationFSM animFSM;

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Lancer;
        lancerBlackBoard = new LancerBlackBoard();
        animFSM = GetComponent<AnimationFSM>();
        bt = CreateBehaviourTree(this);
    }

    private void Update()
    {
        bt?.Tick();
        CheckEnemyDirection();
        UpdateDetectPointPosition();
        animFSM.ChangeState(currentState);
        if(lancerBlackBoard.detectedEnemy != null)
        {
            UpdateDirection(lancerBlackBoard.detectedEnemy.transform.position, transform.position);
        }
        
    }

    #region Behaviour Tree

    private BehaviourTree CreateBehaviourTree(Lancer lancer)
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

  
    
    private void CheckEnemyDirection()
    {
        if (lancerBlackBoard.detectedEnemy == null)
            return;
        
        var distance = lancerBlackBoard.detectedEnemy
            .transform.position - transform.position;
        if (CheckEnemyStillInRange(viewDistance))
        {
            lancerBlackBoard.lastDirection = distance.x > 0 ? Vector2.right : Vector2.left;
            UpdateFacing(lancerBlackBoard.lastDirection);
        }
        else
        {
            lancerBlackBoard.detectedEnemy = null;
            ResetState();
        }
            
    }
    
    public bool CheckEnemyStillInRange(float range)
    {
        int size = Physics2D.OverlapCircleNonAlloc(transform.position, range, results);

        for (int i = 0; i < size; i++)
        {
            if (results[i] != null &&
                results[i].gameObject == lancerBlackBoard.detectedEnemy)
                return true;
        }

        return false;
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
            <= 15f => LancerDirection.Up,
            <= 75f => LancerDirection.UpRight,
            <= 105f => LancerDirection.Right,
            <= 165f => LancerDirection.DownRight,
            _ => LancerDirection.Down
        };
    }

    private void UpdateDirection(Vector2 from, Vector2 to)
    {
        var dir = GetDirection(from, to);
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

        return dist <= attackRange;
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
