using UnityEngine;
using _Script.Task;

namespace _Script.BT.Node.BuilderNode
{
    public class AlignToTargetNode : BTActionNode
    {
        private Builder builder;
        private bool hasStartedAlign = false;

        public AlignToTargetNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentTask == null || builder.currentTask.targetGameObject == null)
            {
                hasStartedAlign = false;
                return BTStatus.Failure;
            }

            // Nếu thực tế cơ thể đã va chạm/sát nút mục tiêu từ trước (ví dụ lúc vừa Load game)
            if (builder.IsCollidingWithTaskTarget())
            {
                hasStartedAlign = false;
                return BTStatus.Success; // Xong luôn không cần bò nữa
            }

            // BẮT ĐẦU ALIGN
            if (!hasStartedAlign)
            {
                hasStartedAlign = true;
                // Gọi hàm tịnh tiến thẳng đến tâm mục tiêu cũ của bạn
                builder.MoveDirectlyToTarget(builder.currentTask.targetGameObject);
            }

            // Kiểm tra điều kiện khoảng cách vật lý của cơ thể
            if (!builder.IsCollidingWithTaskTarget())
            {
                // Vẫn đang bò sát vào
                builder.currentState = UnitState.Move;
                builder.animState = AnimState.Moving;
                return BTStatus.Running;
            }

            // ĐÃ ÁP SÁT THÀNH CÔNG SÁT NÚT
            hasStartedAlign = false;
            
            // Đảm bảo bẻ gãy lực quán tính hoặc tắt trạng thái di chuyển thẳng
            if (builder.characterMovement != null)
            {
                builder.characterMovement.StopMoving();
            }
            
            return BTStatus.Success;
        }
    }
}