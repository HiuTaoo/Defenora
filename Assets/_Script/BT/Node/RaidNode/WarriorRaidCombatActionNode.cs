using _Script.BT.Node;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;
using _Script.BT; // Đảm bảo nạp đúng namespace chứa định nghĩa BTStatus của bạn

public class WarriorRaidCombatActionNode : BTActionNode
{
    private readonly Warrior warrior;
    private Health warriorHealth;

    // Các biến quản lý Cooldown phòng thủ riêng biệt (Block Cooldown)
    private float defendCooldownDuration = 2.0f; // Thời gian hồi khiên thủ (giây)
    private float lastDefendTriggerTime = -999f;
    private float lastFrameHealth;
    private bool isDefendCooldownActive = false;

    public WarriorRaidCombatActionNode(Unit unit) : base(unit)
    {
        warrior = unit as Warrior;
        if (warrior != null)
        {
            warriorHealth = warrior.GetComponentInChildren<Health>();
        }
    }

    public override BTStatus Tick()
    {
        if (warrior == null) return BTStatus.Failure;
        
        if (RaidManager.Instance == null || RaidManager.Instance.activeRaidTarget == null)
        {
            ResetInternalState();
            return BTStatus.Failure;
        }

        if (warrior.isKnockedBack)
        {
            isDefendCooldownActive = false; 
            return BTStatus.Running;
        }

        if (RaidManager.Instance.IsRaidActive)
        {
            if (warrior.currentTarget == null || warrior.warriorBlackBoard.detectedEnemy == null)
            {
                var gate = RaidManager.Instance.activeRaidTarget;
                warrior.warriorBlackBoard.detectedEnemy = gate;
                warrior.currentTarget = gate.transform;

                var spawnPointComponent = gate.GetComponent<SpawnPoint>();
                warrior.currentTargetLayerIndex = spawnPointComponent != null ? spawnPointComponent.layerIndex : 0;

                warrior.aggroTimer = 9999f;
                warrior.isAlerted = true;
            }
        }

        var raidGate = RaidManager.Instance.activeRaidTarget;
        if (raidGate == null || !raidGate.activeInHierarchy)
        {
            ResetInternalState();
            return BTStatus.Success;
        }

        Vector2 dirToGate = (raidGate.transform.position - warrior.transform.position).normalized;
        warrior.warriorBlackBoard.lastDirection = dirToGate.x > 0 ? Vector2.right : Vector2.left;
        warrior.UpdateFacing(warrior.warriorBlackBoard.lastDirection);

        var facingDir = warrior.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        var closeEnemies = warrior.DetectEnemies(warrior.attackRange, facingDir);
        var priorityEnemy = warrior.SelectClosestTarget(closeEnemies);

        if (warriorHealth != null && !isDefendCooldownActive && warrior.currentState == UnitState.Defend)
        {
            if (warriorHealth.CurrentHealth < lastFrameHealth && warriorHealth.CurrentHealth > 0)
            {
                float damageTaken = lastFrameHealth - warriorHealth.CurrentHealth;
                
                warriorHealth.Heal(damageTaken); 

                Debug.LogWarning($"[🛡️ WARRIOR BLOCK] Đỡ thành công {damageTaken} sát thương! Khiên vỡ -> Chuyển sang trạng thái hồi chiêu.");

                isDefendCooldownActive = true;
                lastDefendTriggerTime = Time.time;

                warrior.currentState = UnitState.Idle;
                warrior.animState = AnimState.Idle;
                
                if (warrior.isAttacking) warrior.EndAttackSignal();

                lastFrameHealth = warriorHealth.CurrentHealth;
                return BTStatus.Running;
            }
        }

        if (isDefendCooldownActive)
        {
            if (Time.time >= lastDefendTriggerTime + defendCooldownDuration)
            {
                isDefendCooldownActive = false; 
            }
            else
            {
                if (priorityEnemy != null)
                {
                    warrior.warriorBlackBoard.detectedEnemy = priorityEnemy;
                    warrior.currentTarget = priorityEnemy.transform;

                    if (warrior.isAttacking)
                    {
                        warrior.currentState = UnitState.Idle; 
                        warrior.animState = AnimState.Attacking;
                        if (warriorHealth != null) lastFrameHealth = warriorHealth.CurrentHealth;
                        return BTStatus.Running;
                    }

                    if (Time.time >= warrior.lastAttackTime + warrior.attackCooldown)
                    {
                        if (warrior.characterMovement.moving) warrior.StopMove();

                        warrior.lastAttackTime = Time.time;
                        warrior.StartAttackSignal();

                        warrior.currentState = UnitState.Idle;
                        warrior.animState = AnimState.Attacking;
                    }
                    else
                    {
                        warrior.currentState = UnitState.Idle;
                        warrior.animState = AnimState.Idle;
                    }
                }
                else
                {
                    warrior.currentState = UnitState.Idle;
                    warrior.animState = AnimState.Idle;
                }

                if (warriorHealth != null) lastFrameHealth = warriorHealth.CurrentHealth;
                return BTStatus.Running;
            }
        }

        warrior.warriorBlackBoard.detectedEnemy = null;
        if (warrior.isAttacking) warrior.EndAttackSignal();

        warrior.currentState = UnitState.Defend;
        warrior.animState = AnimState.Defending; 

        if (warriorHealth != null) lastFrameHealth = warriorHealth.CurrentHealth;

        return BTStatus.Running;
    }

    public override void ClearState()
    {
        base.ClearState();
        ResetInternalState();
    }

    private void ResetInternalState()
    {
        isDefendCooldownActive = false;
        if (warrior != null)
        {
            warrior.EndAttackSignal();
            warrior.currentState = UnitState.Idle;
            warrior.animState = AnimState.Idle;
        }
    }
}