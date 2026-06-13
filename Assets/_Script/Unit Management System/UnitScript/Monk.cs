using System.Collections.Generic;
using System.Globalization;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.GlobalAlarm;
using _Script.BT.Node.LancerNode.LancerIdle;
using _Script.BT.Node.MonkNode.MonkIdle;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
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

    public bool isPanicking { get; set; }

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
        if (Mathf.Approximately(Time.timeScale, 0f))
            return;

        base.Update();

        if (!isPanicking)
        {
            var facingDir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

            var enemiesSpotted = DetectEnemies(viewDistance, facingDir);

            if (enemiesSpotted != null && enemiesSpotted.Count > 0)
            {
                var firstEnemy = enemiesSpotted[0];

                Debug.LogWarning(
                    $"[Sensor Update] 🚨 Monk {gameObject.name} nhìn thấy quái vật {firstEnemy.name}! Bật cờ hoảng loạn bỏ chạy!");

                isPanicking = true;
                characterMovement.RequestStopMoving();
                bt?.ClearState();

                monkBlackBoard.detectedEnemy = firstEnemy;

                var enemyFloor = firstEnemy.GetComponentInChildren<FloorAgent>()._currentFloorIndex;
                GlobalAlarmSystem.TriggerAlarm(firstEnemy.gameObject, firstEnemy.transform.position, enemyFloor);
            }
        }

        if (!isPanicking) CheckEnemyDirection();

        bt?.Tick();
        animFSM.ChangeState(currentState, animState);
    }

    #region Behaviour Tree

    private BehaviourTree CreateBehaviorTree(Monk monk)
    {
        var raidCampaignSequence = new SequenceNode(
            new IsInRaidCampaignNode(monk),
            new RaidCampaignExecutionNode(monk),
            new MonkRaidCombatActionNode(monk)
        );
        
        var healAllySequence = new SequenceNode(
            new IsAllyNeedHealNode(monk),
            new SelectorNode(
                new MonkExecuteHealActionNode(monk),
                new MonkMoveToSafeRangeNode(monk)
            )
        );

        var idleSequence = new SequenceNode(
            new IsIdleMonkNode(monk),
            new FindNextPatrolPositionMonkNode(monk),
            new MoveToNextPatrolPositionMonkNode(monk),
            new WaitNode(monk));

        var root = new SelectorNode(
            raidCampaignSequence,
            new MonkPanicFleeActionNode(monk),
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
            if (ally == null || ally.CompareTag("Enemy")) continue;

            var healEffect = PoolManager.Instance.Spawn(PrefabConfig.Instance.healEffectPrefab,
                ally.transform.position, Quaternion.identity);

            healEffect.transform.SetParent(ally.transform);
        }

        AudioManager.Instance.PlaySFX3D(SoundNames.SfxHeal, audioSource);
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
        isPanicking = false;
    }

    protected override void HandleGlobalAlarm(GameObject enemy, Vector3 spottedPosition, int layerIndex)
    {
        if (isPanicking) return;

        if (Vector2.Distance(transform.position, spottedPosition) > hearRange) return;

        Debug.LogWarning(
            $"[Global Alarm Event] 🚨 Monk {gameObject.name} nhận được báo động SOS! Có {enemy.name} tại {spottedPosition}! Bật cờ hoảng loạn bỏ chạy!");

        isPanicking = true;
        monkBlackBoard.detectedEnemy = enemy;

        if (characterMovement != null) characterMovement.RequestStopMoving();

        bt?.ClearState();
    }

    #endregion

#if UNITY_EDITOR
    /*private void DrawVisionCone()
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
    }*/
#endif
}