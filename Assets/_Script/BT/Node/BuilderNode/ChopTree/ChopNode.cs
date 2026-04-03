using UnityEngine;
using _Script.Unit_Management_System.Animation;

namespace _Script.BT.Node.BuilderNode
{
    public class ChopNode : BTActionNode
    {
        private Builder builder;
        private float delay = 0.5f;
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

                    builder.ResetState();
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