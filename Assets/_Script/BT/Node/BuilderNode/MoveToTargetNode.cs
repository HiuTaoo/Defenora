using System.Linq;
using _Script.Task;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class MoveToTargetNode : BTActionNode
    {
        private Builder builder;
        private bool hasStartedMove = false;
        private bool isAligningTarget = false;
        
        // Lưu lại ID hoặc chính Object Task để phát hiện khi Builder đổi sang Task mới liên tiếp
        private global::Task lastProcessedTask = null; 

        public MoveToTargetNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentTask == null || builder.builderBlackBoard.pathFinding == null)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            // KHẮC PHỤC LỖI ĐI CHÉO: Nếu phát hiện Task hiện tại khác với Task ở khung hình trước (Chặt cây liên tiếp)
            // lập tức Reset trạng thái nội bộ của Node để ép nó chạy vào khối lệnh khởi động di chuyển A* (!hasStartedMove)
            if (builder.currentTask != lastProcessedTask)
            {
                ResetNode();
                lastProcessedTask = builder.currentTask;
            }
            
            if (builder.currentInventory.IsFull == false  && builder.currentTask.taskType == TaskType.TransportItem)
            {
                bool hasTask = TaskManager.Instance
                    .GetAvailableTasks()
                    .Any();

                if (hasTask)
                {
                    Debug.Log("Interrupt move transport");
                    
                    TaskManager.Instance.RemoveTask(builder.currentTask);
                    builder.currentTask = null;
                    builder.ResetState();
                    ResetNode();

                    return BTStatus.Failure;
                }
            }

            // ===== START =====
            if (!hasStartedMove)
            {
                builder.UpdateAnim();
                builder.currentState = UnitState.Move;
                
                // Kích hoạt di chuyển theo đường tìm được từ Node trước (A*)
                builder.MoveToTargetPosition(builder.builderBlackBoard.pathFinding);

                hasStartedMove = true;
                isAligningTarget = false;
                return BTStatus.Running;
            }

            // ===== PHASE 1: FOLLOW PATH =====
            if (!isAligningTarget && builder.characterMovement.moving)
                return BTStatus.Running;

            // ===== END PATH → CHECK COLLISION =====
            if (!isAligningTarget && !builder.characterMovement.moving)
            {
                if (!builder.IsCollidingWithTaskTarget())
                {
                    isAligningTarget = true;
                    return BTStatus.Running;
                }
            }

            // ===== PHASE 2: ALIGN X =====
            if (isAligningTarget)
            {
                if (!builder.IsCollidingWithTaskTarget())
                {
                    builder.MoveDirectlyToTarget(builder.currentTask.targetGameObject);
                    return BTStatus.Running;
                }

                FinishMove();
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
            isAligningTarget = false;
        }

        private void FinishMove()
        {
            ResetNode();
        }
    }
}