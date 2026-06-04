using _Script.BT.Node;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public class MonkExecuteHealActionNode : BTActionNode
{
    private readonly Monk monk;
    private float _lastHealTime = -999f;

    public MonkExecuteHealActionNode(Unit unit) : base(unit)
    {
        monk = unit as Monk;
    }

    public override BTStatus Tick()
    {
        if (monk == null) return BTStatus.Failure;

        if (Time.time < _lastHealTime + monk.healCooldown)
        {
            monk.currentState = UnitState.Idle;
            monk.animState = AnimState.Idle;
            return BTStatus.Running;
        }

        var layerMask = LayerMask.GetMask("NPC") | LayerMask.GetMask("Player");
        var size = Physics2D.OverlapCircleNonAlloc(monk.transform.position, monk.healRange, monk.results, layerMask);

        var heavilyHealedAnyAlly = false;

        for (var i = 0; i < size; i++)
        {
            var hit = monk.results[i];
            if (hit == null || hit.gameObject == monk.gameObject || hit.CompareTag("Enemy")) continue;

            var targetHealth = hit.GetComponentInChildren<Health>();
            if (targetHealth == null || targetHealth.CurrentHealth <= 0) continue;

            if (targetHealth.CurrentHealth < targetHealth.maxHealth)
            {
                targetHealth.Heal(monk.healAmount);

                monk.monkBlackBoard.lowHPAlly = hit.gameObject;
                monk.UseSpecialAbility();

                heavilyHealedAnyAlly = true;
            }
        }

        if (heavilyHealedAnyAlly)
        {
            Debug.LogWarning(
                $"[🚨 MONK AOE MAGIC] ✨ {monk.gameObject.name} đã kích hoạt trận pháp hồi máu diện rộng trong bán kính {monk.healRange} ô!");
            _lastHealTime = Time.time;

            monk.currentState = UnitState.Idle;
            monk.animState = AnimState.Idle;
            return BTStatus.Success;
        }

        return BTStatus.Failure;
    }
}