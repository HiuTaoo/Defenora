using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class WaitDurationNode : BTActionNode
    {
        private Builder builder;
        private float duration;
        private float startTime;
        private bool isWaiting = false;

        public WaitDurationNode(Unit unit, float waitDuration) : base(unit)
        {
            builder = (Builder)unit;
            duration = waitDuration;
        }

        public override BTStatus Tick()
        {
            if (!isWaiting)
            {
                startTime = Time.realtimeSinceStartup;
                isWaiting = true;
        
                // Chỉ cần dừng di chuyển và đưa trạng thái thể xác về Idle để đứng chờ
                if (builder.characterMovement != null)
                {
                    builder.characterMovement.StopMoving(); 
                }
        
                // CHỈ ĐỔI TRẠNG THÁI HOẠT ĐỘNG, KHÔNG GỌI ResetState() LÀM MẤT DATA KHÁC!
                builder.currentState = UnitState.Idle;
                builder.animState = AnimState.Idle;
                builder.UpdateAnim();
                
                return BTStatus.Running;
            }

            if (Time.realtimeSinceStartup - startTime >= duration)
            {
                isWaiting = false; 
                return BTStatus.Success; // Đợi xong xuôi, trả quyền điều khiển cho cây BT
            }

            return BTStatus.Running;
        }
    }
}