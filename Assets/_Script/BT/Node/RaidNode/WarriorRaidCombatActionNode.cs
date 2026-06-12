using _Script.BT.Node;
using UnityEngine;

public class WarriorRaidCombatActionNode : BTActionNode
{
    private readonly Warrior warrior;

    public WarriorRaidCombatActionNode(Unit unit) : base(unit)
    {
        warrior = unit as Warrior;
    }

    public override BTStatus Tick()
    {
        if (warrior == null) return BTStatus.Failure;

        // 0. KIỂM TRA ĐIỀU KIỆN AN TOÀN TRƯỚC (Bị đẩy lùi thì ngắt đòn ngay)
        if (warrior.isKnockedBack)
        {
            ResetInternalState();
            return BTStatus.Running; // Trong chế độ Raid, giữ Running để không gãy luồng cây BT
        }

        // ĐOẠN BẢO VỆ AGGRO: Nếu Raid đang diễn ra mà mất dấu mục tiêu, ép nạp lại Cổng quái
        if (RaidManager.Instance != null && RaidManager.Instance.IsRaidActive)
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

        var raidGate = RaidManager.Instance.activeRaidTarget;
        if (raidGate == null || !raidGate.activeInHierarchy)
        {
            ResetInternalState();
            return BTStatus.Success;
        }

        // 1. QUẢN LÝ QUAY MẶT HƯỚNG VỀ PHÍA CỔNG QUÁI (Giữ vững phòng tuyến)
        Vector2 dirToGate = (raidGate.transform.position - warrior.transform.position).normalized;
        warrior.warriorBlackBoard.lastDirection = dirToGate.x > 0 ? Vector2.right : Vector2.left;
        warrior.UpdateFacing(warrior.warriorBlackBoard.lastDirection);

        // 2. QUÉT VÀ PHÂN LOẠI MỤC TIÊU ÁP SÁT
        var facingDir = warrior.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        var closeEnemies = warrior.DetectEnemies(warrior.attackRange, facingDir);
        var priorityEnemy = warrior.SelectClosestTarget(closeEnemies);

        // 3. THỰC THI LOGIC TẤN CÔNG CHUẨN THAM KHẢO TỪ WARRIORATTACKNODE
        if (priorityEnemy != null)
        {
            // Cập nhật Blackboard nhắm vào con quái áp sát
            warrior.warriorBlackBoard.detectedEnemy = priorityEnemy;
            warrior.currentTarget = priorityEnemy.transform;

            // Nếu hoạt ảnh chém đang diễn ra (Animation Event chưa báo kết thúc), giữ nguyên trạng thái
            if (warrior.isAttacking)
            {
                warrior.currentState = UnitState.Defend;
                warrior.animState = AnimState.Attacking;
                return BTStatus.Running;
            }

            // KIỂM TRA COOLDOWN ĐÒN ĐÁNH (Y hệt node Attack mẫu)
            if (Time.time >= warrior.lastAttackTime + warrior.attackCooldown)
            {
                if (warrior.characterMovement.moving) warrior.StopMove();

                warrior.lastAttackTime = Time.time;

                warrior.currentState = UnitState.Defend;
                warrior.animState = AnimState.Attacking;
            }
            else
            {
                // Đang đợi hồi chiêu chém: Đứng thủ thế (Defend - Idle) hướng về địch
                warrior.currentState = UnitState.Defend;
                warrior.animState = AnimState.Idle;
            }
        }
        else
        {
            // KHÔNG CÓ ĐỊCH ÁP SÁT: Đứng im thủ thế phòng tuyến nhìn về phía cổng quái
            warrior.warriorBlackBoard.detectedEnemy = null;
            warrior.currentTarget = null;

            if (warrior.isAttacking) warrior.EndAttackSignal();

            warrior.currentState = UnitState.Idle;
            warrior.animState = AnimState.Idle;
        }

        return BTStatus.Running;
    }

    public override void ClearState()
    {
        base.ClearState();
        ResetInternalState();
    }

    private void ResetInternalState()
    {
        if (warrior != null)
        {
            warrior.EndAttackSignal();
            warrior.currentState = UnitState.Idle;
            warrior.animState = AnimState.Idle;
        }
    }
}