using _Script.BT.Node;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

public class MonkExecuteHealActionNode : BTActionNode
{
    private readonly Monk monk;
    private float _lastHealTime = -999f;
    
    // Đã dọn dẹp sạch sẽ các biến đếm Timer thủ công cũ!
    private bool hasHealed;
    private int lastFrameChecked = -1;
    private BTStatus lastStatus = BTStatus.Running;

    public MonkExecuteHealActionNode(Unit unit) : base(unit)
    {
        monk = unit as Monk;
    }

    public override BTStatus Tick()
    {
        if (monk == null) return BTStatus.Failure;

        if (Time.time < _lastHealTime + monk.healCooldown && !hasHealed)
        {
            monk.currentState = UnitState.Idle;
            monk.animState = AnimState.Idle;
            return BTStatus.Failure;
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

            lastStatus = BTStatus.Success;
            return BTStatus.Success; 
        }

        if (!hasHealed)
        {
            var layerMask = LayerMask.GetMask("NPC") | LayerMask.GetMask("Player");
            var size = Physics2D.OverlapCircleNonAlloc(monk.transform.position, monk.healRange, monk.results, layerMask);

            var heavilyHealedAnyAlly = false;

            monk.monkBlackBoard.aoeHealTargets.Clear(); 

            for (var i = 0; i < size; i++)
            {
                var hit = monk.results[i];
                if (hit == null || hit.gameObject == monk.gameObject || hit.CompareTag("Enemy")) continue;

                var targetHealth = hit.GetComponentInChildren<Health>();
                if (targetHealth == null || targetHealth.CurrentHealth <= 0) continue;

                if (targetHealth.CurrentHealth < targetHealth.maxHealth)
                {
                    targetHealth.Heal(monk.healAmount);

                    monk.monkBlackBoard.aoeHealTargets.Add(hit.gameObject);
                    
                    monk.monkBlackBoard.lowHPAlly = hit.gameObject;
                    heavilyHealedAnyAlly = true;
                }
            }

            if (heavilyHealedAnyAlly)
            {
                Debug.LogWarning($"[🚨 MONK AOE MAGIC] ✨ {monk.gameObject.name} đã kích hoạt trận pháp hồi máu diện rộng trong bán kính {monk.healRange} ô!");
                
                monk.StartAttackSignal();

                monk.isAlerted = false;
                monk.lastSeenPosition = Vector2.zero;
                monk.lastSeenLayerIndex = -1;

                monk.currentState = UnitState.Heal;
                monk.animState = AnimState.Heal;

                hasHealed = true;
                lastStatus = BTStatus.Running;
                return BTStatus.Running;
            }
            else
            {
                ResetInternal();
                lastStatus = BTStatus.Failure;
                return BTStatus.Failure;
            }
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