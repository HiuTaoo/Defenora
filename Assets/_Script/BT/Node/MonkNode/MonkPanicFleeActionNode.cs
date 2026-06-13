using _Script.BT.Node;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public class MonkPanicFleeActionNode : BTActionNode
{
    private readonly Monk monk;
    private float _panicTimer;
    private bool _hasDestination;

    public MonkPanicFleeActionNode(Unit unit) : base(unit)
    {
        monk = unit as Monk;
    }

    public override BTStatus Tick()
    {
        if (monk == null) return BTStatus.Failure;

        if (!monk.isPanicking)
        {
            ResetNodeData();
            return BTStatus.Failure;
        }

        // =================================================================
        // 🚑 PHÂN HỆ 1: QUÉT TÌM ĐỒNG ĐỘI YẾU MÁU (NON-ALLOC CHUẨN)
        // =================================================================
        var size = Physics2D.OverlapCircleNonAlloc(monk.transform.position, monk.viewDistance, monk.results,
            LayerMask.GetMask("NPC"));
        var highlyInjuredAllyFound = false;
        monk.monkBlackBoard.aoeHealTargets.Clear();

        for (var i = 0; i < size; i++)
        {
            var hit = monk.results[i];
            if (hit == null || hit.gameObject == monk.gameObject) continue;
            if (hit.CompareTag("Enemy")) continue;

            var allyHealth = hit.GetComponentInChildren<Health>();
            if (allyHealth != null && allyHealth.CurrentHealth > 0 &&
                allyHealth.CurrentHealth < allyHealth.maxHealth * 0.95f)
            {
                monk.monkBlackBoard.aoeHealTargets.Add(hit.gameObject);
                highlyInjuredAllyFound = true;
            }
        }

        if (highlyInjuredAllyFound)
        {
            if (monk.characterMovement.moving) monk.StopMove();

            if (Time.time >= monk.lastAttackTime + monk.healCooldown)
            {
                Debug.LogWarning(
                    $"[🚨 PANIC HEAL] Monk {monk.gameObject.name} đang chạy loạn nhưng dừng lại cứu thương cho {monk.monkBlackBoard.aoeHealTargets.Count} đồng đội!");

                monk.lastAttackTime = Time.time;
                monk.StartAttackSignal();

                monk.UseSpecialAbility();

                foreach (var allyObj in monk.monkBlackBoard.aoeHealTargets)
                    if (allyObj != null)
                    {
                        var hp = allyObj.GetComponentInChildren<Health>();
                        if (hp != null) hp.Heal(monk.healAmount);
                    }
            }

            monk.currentState = UnitState.Heal;
            monk.animState = AnimState.Heal;

            _hasDestination = false;

            return BTStatus.Running;
        }

        // =================================================================
        // 🏃 PHÂN HỆ 2: TIẾN TRÌNH CHẠY LOẠN & KIỂM TRA QUÁI BÁM ĐUÔI
        // =================================================================
        _panicTimer += Time.deltaTime;

        if (_panicTimer >= 5f)
        {
            var enemyStillAround = false;

            var size1 = Physics2D.OverlapCircleNonAlloc(monk.transform.position, monk.viewDistance, monk.results,
                LayerMask.GetMask("NPC"));

            for (var i = 0; i < size1; i++)
            {
                var hit = monk.results[i];
                if (hit != null && hit.CompareTag("Enemy"))
                {
                    enemyStillAround = true;
                    break;
                }
            }

            if (!enemyStillAround)
            {
                if (monk.characterMovement != null) monk.characterMovement.RequestStopMoving();
                monk.ResetState();
                monk.GetBT()?.ClearState();

                ResetNodeData();
                return BTStatus.Success;
            }

            _panicTimer = 0f;
            _hasDestination = false;
        }

        if (!_hasDestination || (monk.characterMovement != null && !monk.characterMovement.moving))
        {
            if (monk.isAttacking) monk.EndAttackSignal();

            var currentGridPos = GraphNode.Instance.WorldToGridPos(monk.transform.position, monk.layerIndex);
            var randomOffset = new Vector3Int(Random.Range(-4, 5), Random.Range(-4, 5), 0);
            var targetGridPos = currentGridPos + randomOffset;

            var node = GraphNode.Instance.GetNode(targetGridPos, monk.layerIndex);
            if (node != null && node.isWalkable)
            {
                var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(currentGridPos, monk.layerIndex,
                    targetGridPos, monk.layerIndex);
                if (path != null && path.segments.Count > 0)
                {
                    if (monk.characterMovement != null) monk.characterMovement.RequestStopMoving();
                    monk.MoveToTargetPosition(path);
                    _hasDestination = true;
                }
            }
        }

        monk.currentState = UnitState.Move;
        monk.animState = AnimState.Moving;

        return BTStatus.Running;
    }

    public override void ClearState()
    {
        base.ClearState();
        ResetNodeData();
    }

    private void ResetNodeData()
    {
        _panicTimer = 0f;
        _hasDestination = false;
    }
}