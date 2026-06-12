using _Script.BT.Node;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public class MonkRaidCombatActionNode : BTActionNode
{
    private readonly Monk monk;
    private float _lastHealTime = -999f;

    private bool hasHealed;
    private int lastFrameChecked = -1;
    private BTStatus lastStatus = BTStatus.Running;

    public MonkRaidCombatActionNode(Unit unit) : base(unit)
    {
        monk = unit as Monk;
    }

    public override BTStatus Tick()
    {
        if (monk == null) return BTStatus.Failure;

        if (monk.currentTarget != null)
        {
            var targetGo = monk.currentTarget.gameObject;
            var targetHealth = targetGo.GetComponentInChildren<Health>();

            if (!targetGo.activeInHierarchy || (targetHealth != null && targetHealth.IsDead))
            {
                monk.currentTarget = null;
                monk.currentTargetLayerIndex = -1;

                if (monk.monkBlackBoard != null)
                {
                    monk.monkBlackBoard.lowHPAlly = null;
                    monk.monkBlackBoard.aoeHealTargets.Clear();
                }

                if (hasHealed)
                {
                    monk.EndAttackSignal();
                    monk.ResetAnim();
                    monk.currentState = UnitState.Idle;
                    monk.animState = AnimState.Idle;
                }

                ResetInternal();
                lastStatus = BTStatus.Running;
                return BTStatus.Running;
            }
        }

        if (Time.time < _lastHealTime + monk.healCooldown && !hasHealed)
        {
            monk.currentState = UnitState.Idle;
            monk.animState = AnimState.Idle;
            return BTStatus.Running;
        }

        if (Time.frameCount == lastFrameChecked)
        {
            if (lastStatus == BTStatus.Running)
            {
                monk.currentState = UnitState.Heal;
                monk.animState = AnimState.Heal;
            }

            return lastStatus;
        }

        lastFrameChecked = Time.frameCount;

        if (hasHealed && monk.isAttacking)
        {
            monk.currentState = UnitState.Heal;
            monk.animState = AnimState.Heal;
            lastStatus = BTStatus.Running;
            return BTStatus.Running;
        }

        if (hasHealed && !monk.isAttacking)
        {
            _lastHealTime = Time.time;

            monk.EndAttackSignal();
            monk.ResetAnim();
            monk.currentState = UnitState.Idle;
            monk.animState = AnimState.Idle;

            ResetInternal();

            lastStatus = BTStatus.Running;
            return BTStatus.Running;
        }

        if (!hasHealed)
        {
            var layerMask = LayerMask.GetMask("NPC");
            var size = Physics2D.OverlapCircleNonAlloc(monk.transform.position, monk.viewDistance, monk.results,
                layerMask);

            var heavilyHealedAnyAlly = false;
            monk.monkBlackBoard.aoeHealTargets.Clear();

            for (var i = 0; i < size; i++)
            {
                var hit = monk.results[i];

                if (hit == null || hit.gameObject == monk.gameObject) continue;
                if (hit.CompareTag("Enemy") || hit.gameObject.layer == LayerMask.NameToLayer("SpawnPoint")) continue;

                var targetHealth = hit.GetComponentInChildren<Health>();
                if (targetHealth == null || targetHealth.CurrentHealth <= 0) continue;

                if (targetHealth.CurrentHealth < targetHealth.maxHealth * 0.95f)
                {
                    monk.monkBlackBoard.aoeHealTargets.Add(hit.gameObject);
                    monk.monkBlackBoard.lowHPAlly = hit.gameObject;

                    monk.currentTarget = hit.transform;

                    heavilyHealedAnyAlly = true;
                }
            }

            if (heavilyHealedAnyAlly)
            {
                Debug.LogWarning(
                    $"[🚨 RAID HEAL] ✨ Monk {monk.gameObject.name} kích hoạt trận pháp cứu trợ đồng đội trong chiến dịch!");

                monk.StartAttackSignal();

                foreach (var allyObj in monk.monkBlackBoard.aoeHealTargets)
                    if (allyObj != null && !allyObj.CompareTag("Enemy"))
                    {
                        var hp = allyObj.GetComponentInChildren<Health>();
                        if (hp != null) hp.Heal(monk.healAmount);
                    }

                monk.UseSpecialAbility();

                monk.currentState = UnitState.Heal;
                monk.animState = AnimState.Heal;

                hasHealed = true;
                lastStatus = BTStatus.Running;
                return BTStatus.Running;
            }

            monk.StopMove();
            monk.currentState = UnitState.Idle;
            monk.animState = AnimState.Idle;

            ResetInternal();
            lastStatus = BTStatus.Running;
            return BTStatus.Running;
        }

        return BTStatus.Running;
    }

    public override void ClearState()
    {
        base.ClearState();
        if (monk != null && hasHealed)
        {
            monk.EndAttackSignal();
            monk.ResetAnim();
            monk.currentState = UnitState.Idle;
            monk.animState = AnimState.Idle;
        }

        ResetInternal();
    }

    private void ResetInternal()
    {
        hasHealed = false;
        lastFrameChecked = -1;
        lastStatus = BTStatus.Running;
    }
}