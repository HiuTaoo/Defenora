using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.TNTGoblinNode
{
    public class TNTGoblinAttackPlayerNode : BTActionNode
    {
        private TNTGoblin tntGoblin;

        public TNTGoblinAttackPlayerNode(Unit unit) : base(unit)
        {
            tntGoblin = unit as TNTGoblin;
        }
        
        public override BTStatus Tick()
        {
            // 1. Kiểm tra trạng thái bị đẩy lùi
            if (tntGoblin.isKnockedBack)
            {
                ResetState();
                return BTStatus.Failure;
            }

            // 2. Bảo hiểm Null và kiểm tra chắc chắn mục tiêu hiện tại là Player
            if (tntGoblin.currentTarget == null || !tntGoblin.currentTarget.gameObject.activeInHierarchy ||
                !tntGoblin.currentTarget.CompareTag("Player"))
            {
                ResetState();
                return BTStatus.Failure;
            }

            // 🟢 KHÁC BIỆT CHÍ MẠNG: Đã loại bỏ hoàn toàn đoạn check GetComponent<Unit>().currentState của NPC
            // Nhường quyền sinh tử cho lớp quản lý máu của Player (hoặc hệ thống dọn mục tiêu tự xử lý)

            // 3. Tính toán khoảng cách thông minh dựa theo rìa Collider của Player (Giống NPC 100%)
            var targetCol = tntGoblin.currentTarget.GetComponent<Collider2D>();
            float dist;

            if (targetCol != null)
            {
                var closestPoint = targetCol.ClosestPoint(tntGoblin.transform.position);
                dist = Vector2.Distance(tntGoblin.transform.position, closestPoint);
            }
            else
            {
                dist = Vector2.Distance(tntGoblin.transform.position, tntGoblin.currentTarget.transform.position);
            }

            // 4. Nếu Player lọt ra khỏi tầm ném xa -> Báo Thất bại để nhường Sequence rượt đuổi
            if (dist > tntGoblin.attackRange)
            {
                ResetState();
                return BTStatus.Failure;
            }

            // Vào tầm ném bom: Ép đứng im tại chỗ
            tntGoblin.characterMovement.RequestStopMoving();

            // Nếu animation frame trước vẫn đang vung tay ném dở
            if (tntGoblin.isAttacking)
            {
                return BTStatus.Running;
            }

            // 5. Kiểm tra hồi chiêu và xả bộc phá thời gian thực
            if (Time.time >= tntGoblin.lastAttackTime + tntGoblin.attackCooldown)
            {
                tntGoblin.lastAttackTime = Time.time;
                tntGoblin.StartAttackSignal(); // Phát tín hiệu vung bom

                tntGoblin.currentState = UnitState.Attack;
                tntGoblin.animState = AnimState.Attacking;
            }
            else
            {
                // Đang hồi chiêu thì đứng bật Idle ngắm Player
                tntGoblin.currentState = UnitState.Idle;
                tntGoblin.animState = AnimState.Idle;
            }
            
            return BTStatus.Running;
        }
        
        private void ResetState()
        {
            tntGoblin.EndAttackSignal();
            tntGoblin.currentState = UnitState.Idle;
            tntGoblin.animState = AnimState.Idle;
        }
    }
}