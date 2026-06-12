using _Script.Unit_Management_System.Animation;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;
using _Script.BT; // Đảm bảo nạp đúng namespace chứa BTStatus

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class ArcherRaidCombatActionNode : BTActionNode
    {
        private readonly Archer archer;
        private bool hasShot;

        private int lastFrameChecked = -1;
        private BTStatus lastStatus = BTStatus.Running;

        public ArcherRaidCombatActionNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if (archer == null) return BTStatus.Failure;
            
            // BỘ LỌC AN TOÀN: Nếu chiến dịch Raid tổng kết thúc, lập tức thoát Node![cite: 1, 2]
            if (RaidManager.Instance == null || RaidManager.Instance.activeRaidTarget == null)
            {
                ResetInternal();
                return BTStatus.Failure;
            }

            // Tự động gán mục tiêu mặc định là Cổng quái nếu rảnh tay
            if (RaidManager.Instance.IsRaidActive)
                if (archer.currentTarget == null || archer.archerBlackBoard.detectedEnemy == null)
                {
                    var gate = RaidManager.Instance.activeRaidTarget;

                    archer.archerBlackBoard.detectedEnemy = gate;
                    archer.currentTarget = gate.transform;

                    var spawnPointComponent = gate.GetComponent<SpawnPoint>();
                    archer.currentTargetLayerIndex = spawnPointComponent != null ? spawnPointComponent.layerIndex : 0;

                    archer.aggroTimer = 9999f;
                    archer.isAlerted = true;
                }

            // XỬ LÝ KHI CHƯA BẮN (QU TÌM MỤC TIÊU)
            if (!hasShot)
            {
                // Nếu tốc bắn chưa hồi xong, trả về Success để giải phóng frame, tránh đóng băng não
                if (Time.time < archer.nextFireTime)
                {
                    if (archer.characterMovement.moving) archer.StopMove();
                    archer.currentState = UnitState.Idle;
                    archer.animState = AnimState.Idle;
                    return BTStatus.Success; 
                }

                var gate = RaidManager.Instance.activeRaidTarget;
                if (gate == null || !gate.activeInHierarchy)
                {
                    ResetInternal();
                    return BTStatus.Failure;
                }

                // --- LOGIC ƯU TIÊN DIỆT ĐỊCH TRƯỚC ---
                var facingDir = archer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                var enemiesInSight = archer.DetectEnemies(archer.attackRange, facingDir);
                var targetEnemy = archer.SelectClosestTarget(enemiesInSight);

                // Ưu tiên lấy lính địch (targetEnemy), nếu không có mới bắn Cổng (gate)
                var finalTarget = targetEnemy != null ? targetEnemy : gate;

                var targetHealth = finalTarget.GetComponentInChildren<Health>();
                if (targetHealth != null && targetHealth.IsDead)
                {
                    ResetInternal();
                    return BTStatus.Success;
                }

                // ĐÃ SỬA CHUẨN: Kiểm tra khoảng cách dựa trên TẦM BẮN (attackRange) chứ không phải tầm nhìn!
                var currentDist = Vector2.Distance(archer.transform.position, finalTarget.transform.position);
                if (currentDist > archer.attackRange)
                {
                    if (archer.characterMovement.moving) archer.StopMove();
                    archer.currentState = UnitState.Idle;
                    archer.animState = AnimState.Idle;
                    ResetInternal();
                    return BTStatus.Success; // Trả về Success để nhường cây BT tính toán lại vị trí đứng
                }

                // Xoay mặt và nạp hướng bắn
                Vector2 distance = finalTarget.transform.position - archer.transform.position;
                archer.archerBlackBoard.lastDirection = distance.x > 0 ? Vector2.right : Vector2.left;
                archer.UpdateFacing(archer.archerBlackBoard.lastDirection);

                archer.archerBlackBoard.detectedEnemy = finalTarget;
                archer.currentTarget = finalTarget.transform;

                var calculatedFireDir = archer.GetFireDirection(archer.transform.position, finalTarget.transform.position);
                archer.archerBlackBoard.fireDirection = calculatedFireDir;
            }
            else // ĐÃ BẮN VÀ ĐANG THEO DÕI MỤC TIÊU CŨ
            {
                var target = archer.archerBlackBoard.detectedEnemy;
                if (target != null)
                {
                    var targetHealth = target.GetComponentInChildren<Health>();
                    if (!target.activeInHierarchy || (targetHealth != null && targetHealth.IsDead))
                    {
                        archer.EndAttackSignal();
                        archer.ResetAnim();
                        archer.currentState = UnitState.Idle;
                        archer.animState = AnimState.Idle;

                        archer.archerBlackBoard.detectedEnemy = null;
                        ResetInternal();
                        return BTStatus.Success;
                    }
                }
            }

            // Đồng bộ Animation theo Frame của Unity
            if (Time.frameCount == lastFrameChecked)
            {
                if (lastStatus == BTStatus.Running && hasShot)
                {
                    var fireDir = archer.archerBlackBoard.fireDirection;
                    if (fireDir != ArcherFireDirection.None) archer.animFSM.SetFireDirection(fireDir);

                    archer.animState = AnimState.Attacking;
                    archer.currentState = UnitState.Attack;
                }
                return lastStatus;
            }

            lastFrameChecked = Time.frameCount;

            // Tiến trình hoạt ảnh bắn đang diễn ra -> Giữ Running
            if (hasShot && archer.isAttacking)
            {
                archer.currentState = UnitState.Attack;
                archer.animState = AnimState.Attacking;
                lastStatus = BTStatus.Running;
                return BTStatus.Running;
            }

            // Hoàn thành 1 mũi tên bắn ra -> Vào thời gian Cooldown
            if (hasShot && !archer.isAttacking)
            {
                archer.nextFireTime = Time.time + archer.attackCooldown;

                archer.EndAttackSignal();
                archer.ResetAnim();
                archer.currentState = UnitState.Idle;
                archer.animState = AnimState.Idle;

                ResetInternal();
                return BTStatus.Success; // Bắn xong 1 mũi -> trả về Success để chu kỳ sau quét lại tìm mục tiêu mới
            }

            // Bắt đầu lệnh kích hoạt giương cung bắn
            if (!hasShot)
            {
                if (archer.characterMovement.moving) archer.StopMove();

                var fireDir = archer.archerBlackBoard.fireDirection;
                if (fireDir != ArcherFireDirection.None) archer.animFSM.SetFireDirection(fireDir);

                archer.StartAttackSignal();

                archer.currentState = UnitState.Attack;
                archer.animState = AnimState.Attacking;

                hasShot = true;
                lastStatus = BTStatus.Running;
                return BTStatus.Running;
            }

            return BTStatus.Success;
        }

        public override void ClearState()
        {
            base.ClearState();
            if (archer != null && hasShot)
            {
                archer.EndAttackSignal();
                archer.ResetAnim();
                archer.currentState = UnitState.Idle;
                archer.animState = AnimState.Idle;
            }
            ResetInternal();
        }

        private void ResetInternal()
        {
            hasShot = false;
            lastFrameChecked = -1;
            lastStatus = BTStatus.Running;
        }
    }
}