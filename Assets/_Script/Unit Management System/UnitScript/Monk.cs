using System.Collections.Generic;
using System.Globalization;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.Node.LancerNode.LancerIdle;
using _Script.BT.Node.MonkNode.MonkIdle;
using _Script.BT.Node.WarriorNode.WarriorCombat.WarriorArlert;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using UnityEditor;
using UnityEngine;

public class Monk : Unit
{
    public float healCooldown => unitStatsManager != null ? unitStatsManager.AttackCooldown : 3;
    private float nextHealTime;

    public float healAmount
    {
        get
        {
            if (unitStatsManager.GetBaseData() is MonkStatsSO monkData)
            {
                var levelMultiplier = unitStatsManager.currentLevel - 1;
                return monkData.baseHealAmount + monkData.healAmountPerLevel * levelMultiplier;
            }

            Debug.LogError($"[Builder] Quên gắn file BuilderStatsSO cho {gameObject.name}!");
            return 0f;
        }
    }

    public float healRange
    {
        get
        {
            if (unitStatsManager.GetBaseData() is MonkStatsSO monkData)
            {
                var levelMultiplier = unitStatsManager.currentLevel - 1;
                return monkData.baseHealRange + monkData.healRangePerLevel * levelMultiplier;
            }

            Debug.LogError($"[Builder] Quên gắn file BuilderStatsSO cho {gameObject.name}!");
            return 0f;
        }
    }

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
        UpdateSensors();
        bt?.Tick();
        animFSM.ChangeState(currentState, animState);
        CheckEnemyDirection();
    }

    #region Behaviout Tree

    private BehaviourTree CreateBehaviorTree(Monk monk)
    {
        // =================================================================
        // 🚑 PHÂN HỆ CẤP CỨU ĐỒNG ĐỘI (ƯU TIÊN TỐI CAO)
        // =================================================================
        var healAllySequence = new SequenceNode(
            new IsAllyNeedHealNode(monk),
            new SelectorNode(
                new MonkExecuteHealActionNode(monk),
                new MonkMoveToSafeRangeNode(monk)
            )
        );

        // =================================================================
        // 🌲 PHÂN HỆ ĐI DẠO HÒA BÌNH (MOBILE IDLE)
        // =================================================================
        var idleSequence = new SequenceNode(
            new IsIdleMonkNode(monk),
            new FindNextPatrolPositionMonkNode(monk),
            new MoveToNextPatrolPositionMonkNode(monk),
            new WaitNode(monk));

        var root = new SelectorNode(
            healAllySequence,
            idleSequence      
        );
        
        return new BehaviourTree(root);
    }

    #endregion

    public override void UseSpecialAbility()
    {
        if (monkBlackBoard.aoeHealTargets == null || monkBlackBoard.aoeHealTargets.Count == 0) 
            return;

        foreach (var ally in monkBlackBoard.aoeHealTargets)
        {
            if (ally == null) continue;

            var healEffect = PoolManager.Instance.Spawn(PrefabConfig.Instance.healEffectPrefab,
                ally.transform.position, Quaternion.identity);

            healEffect.transform.SetParent(ally.transform);
        }
    }

    public override List<(string name, string value)> GetSpecialStats()
    {
        var extraStats = new List<(string name, string value)>();

        extraStats.Add(("Heal Amount", healAmount.ToString(CultureInfo.InvariantCulture)));
        extraStats.Add(("Heal Range", healRange.ToString(CultureInfo.InvariantCulture)));
        extraStats.Add(("Heal Cooldown", healCooldown.ToString(CultureInfo.InvariantCulture)));

        return extraStats;
    }

    #region Method

    public List<GameObject> DetectEnemies(float range, Vector2 dir)
    {
        var enemiesInRange = new List<GameObject>();

        var size = Physics2D.OverlapCircleNonAlloc(
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

            var b = hit.bounds;
            Vector2[] samplePoints =
            {
                b.center,
                new(b.center.x, b.max.y),
                new(b.center.x, b.min.y),
                new(b.min.x, b.center.y),
                new(b.max.x, b.center.y)
            };

            var visible = false;

            foreach (var point in samplePoints)
            {
                var dirRay = point - myPos;
                var dist = dirRay.magnitude;
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

            if (visible) enemiesInRange.Add(hit.gameObject);
        }

        return enemiesInRange;
    }

    public bool CheckEnemyStillInRange(float range)
    {
        var size = Physics2D.OverlapCircleNonAlloc(transform.position, range, results);

        for (var i = 0; i < size; i++)
            if (results[i] != null &&
                results[i].gameObject == monkBlackBoard.detectedEnemy)
                return true;

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
    
    private void UpdateSensors()
    {
        detectTimer += Time.deltaTime; 
        if (detectTimer >= detectInterval) 
        {
            detectTimer = 0f;

            if (isAlerted)
            {
                var dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                var enemies = DetectEnemies(viewDistance, dir);

                var closestEnemy = SelectClosestTarget(enemies);

                if (closestEnemy != null)
                {
                    monkBlackBoard.detectedEnemy = closestEnemy;
                    
                    lastSeenPosition = closestEnemy.transform.position; 
                }
                else
                {
                    Debug.Log($"[🧘 MONK SENSOR] 🛡️ Sạch bóng quân thù trong tầm rada! Tự động hạ cờ báo động khẩn cấp.");
                    
                    isAlerted = false; // Hạ cờ an toàn!
                    lastSeenPosition = Vector2.zero;
                    lastSeenLayerIndex = -1;
                    monkBlackBoard.detectedEnemy = null;
                    
                    bt?.ClearState(); 
                    ResetState();
                }
            }
        }
    }

    #endregion

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
        var origin = transform.position;
        var direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;

        Handles.color = new Color(1, 1, 0, 0.2f);
        Handles.DrawSolidArc(
            origin,
            Vector3.forward,
            Quaternion.Euler(0, 0, -viewAngle / 2) * direction,
            viewAngle,
            viewDistance
        );

        Gizmos.color = Color.yellow;

        var leftBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * direction;
        var rightBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * direction;

        Gizmos.DrawLine(origin, origin + leftBoundary * viewDistance);
        Gizmos.DrawLine(origin, origin + rightBoundary * viewDistance);
    }

#endif
}