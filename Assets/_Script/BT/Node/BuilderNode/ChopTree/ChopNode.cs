using _Script.Task;
using UnityEngine;
using _Script.Unit_Management_System.Animation;

namespace _Script.BT.Node.BuilderNode
{
    public class ChopNode : BTActionNode
    {
        private Builder builder;
        private float delay = 1f;
        private float timer = 0f;
        private bool isFinishing = false;

        public ChopNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (!isFinishing)
            {
                if (builder.currentState != UnitState.Working)
                {
                    builder.currentState = UnitState.Working;
                    builder.animState = AnimState.Working;
                    builder.currentTool = ToolType.Axe;
                    builder.currentResource = ResourceType.None;
                    builder.UpdateAnim();
                }

                if (builder.IsChopped())
                {
                    isFinishing = true;
                    timer = 0f;

                    // 🟢 SỬA LOGIC CHÍ MẠNG: Kiểm tra xem loại Task hiện tại có phải là Chặt cây tài nguyên không
                    if (builder.currentTask != null && builder.currentTask.taskType == TaskType.ChopTree)
                    {
                        // Nếu đúng là Task chặt cây thì mới được phép xóa Task, giải phóng Builder
                        builder.ResetState();
                    }
                    else
                    {
                        // Nếu chỉ là dọn vật cản (để Xây nhà hoặc Sửa nhà), TUYỆT ĐỐI không gọi ResetState().
                        // Chỉ reset trạng thái cơ thể về Idle để chuỗi xây dựng chạy tiếp, bảo toàn currentTask!
                        builder.currentState = UnitState.Idle;
                        builder.animState = AnimState.Idle;
                        builder.UpdateAnim();
                    }
                }

                return BTStatus.Running;
            }
            
            timer += Time.deltaTime;

            if (timer < delay)
                return BTStatus.Running;
            
            isFinishing = false;
            timer = 0f;

            return BTStatus.Success;
        }
    }
}