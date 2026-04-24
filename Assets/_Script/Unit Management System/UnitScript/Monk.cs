using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.Node.LancerNode.LancerIdle;
using _Script.BT.Node.MonkNode.MonkIdle;
using _Script.ScriptableObjectScript;
using _Script.Unit_Management_System.Animation;
using UnityEditor;
using UnityEngine;

public class Monk : Unit
{
    [Header("Priest Specific")] 
    public float healCooldown;
    public float healAmount
    {
        get
        {
            if (statsManager.GetBaseData() is MonkStatsSO monkData)
            {
                int levelMultiplier = statsManager.currentLevel - 1;
                return monkData.baseHealAmount + (monkData.healAmountPerLevel * levelMultiplier);
            }
            
            Debug.LogError($"[Builder] Quên gắn file BuilderStatsSO cho {gameObject.name}!");
            return 0f; 
        }
    }
    
    public float healRange
    {
        get
        {
            if (statsManager.GetBaseData() is MonkStatsSO monkData)
            {
                int levelMultiplier = statsManager.currentLevel - 1;
                return monkData.baseHealRange + (monkData.healRangePerLevel * levelMultiplier);
            }
            
            Debug.LogError($"[Builder] Quên gắn file BuilderStatsSO cho {gameObject.name}!");
            return 0f; 
        }
    }
    private float nextHealTime;

    public MonkBlackBoard monkBlackBoard;

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Monk;
        bt = CreateBehaviorTree(this);
        monkBlackBoard = new MonkBlackBoard();
 
    }

    protected override void Update()
    {
        base.Update();
        bt?.Tick();
        animFSM.ChangeState(currentState, animState);
        CheckEnemyDirection();
    }

    #region Behaviout Tree

    private BehaviourTree CreateBehaviorTree(Monk monk)
    {
        var idleSequence = new SequenceNode(
            new IsIdleMonkNode(monk),
            new FindNextPatrolPositionMonkNode(monk),
            new MoveToNextPatrolPositionMonkNode(monk),
            new WaitNode(monk));

        var root = new SelectorNode(
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
    
    public bool CheckEnemyStillInRange(float range)
    {
        int size = Physics2D.OverlapCircleNonAlloc(transform.position, range, results);

        for (int i = 0; i < size; i++)
        {
            if (results[i] != null &&
                results[i].gameObject == monkBlackBoard.detectedEnemy)
                return true;
        }

        return false;
    }
    
    private void CheckEnemyDirection()
    {
        if (monkBlackBoard.detectedEnemy == null)
            return;
        
        var distance = monkBlackBoard.detectedEnemy
            .transform.position - transform.position;
        if (CheckEnemyStillInRange(viewDistance))
        {
            monkBlackBoard.lastDirection = distance.x > 0 ? Vector2.right : Vector2.left;
            UpdateFacing(monkBlackBoard.lastDirection);
        }
        else
        {
            monkBlackBoard.detectedEnemy = null;
            ResetState();
        }
            
    }
    
    public void ResetState()
    {
        currentState = UnitState.Idle;
        animState = AnimState.Idle;
    }

    #endregion

    public override void UseSpecialAbility()
    {
    }
    
    public override List<(string name, string value)> GetSpecialStats()
    {
        var extraStats = new List<(string name, string value)>();
        
        extraStats.Add(("Heal Amount", healAmount.ToString(CultureInfo.InvariantCulture))); 
        extraStats.Add(("Heal Range", healRange.ToString(CultureInfo.InvariantCulture)));
        extraStats.Add(("Heal Cooldown", healCooldown.ToString(CultureInfo.InvariantCulture)));
        
        return extraStats;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        if (monkBlackBoard == null)
            return;

        if (monkBlackBoard.detectedEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, monkBlackBoard.detectedEnemy.transform.position);

            Gizmos.DrawSphere(monkBlackBoard.detectedEnemy.transform.position, 0.1f);
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

