using UnityEngine;
using _Script.Unit_Management_System.Animation;

namespace _Script.BT.Node.BuilderNode
{
    public class ChopNode : BTActionNode
    {
        private float delay = 0.5f;
        private float timer = 0f;
        private bool isFinishing = false;

        public ChopNode(Builder builder) : base(builder) { }

        public override BTStatus Tick()
        {
            if (!isFinishing)
            {
                if (builder.currentState != UnitState.Working)
                {
                    builder.currentState = UnitState.Working;
                    builder.currentTool = ToolType.Axe;
                    builder.UpdateAnim();
                    builder.animFSM.ChangeState(UnitState.Working);
                }

                if (builder.IsChopped())
                {
                    isFinishing = true;
                    timer = 0f;

                    builder.currentResource = ResourceType.None;
                    builder.UpdateAnim();
                    builder.currentState = UnitState.Idle;
                    builder.animFSM.ChangeState(UnitState.Idle);
                    builder.targetGO = null;
                    builder.builderBlackBoard.pathFinding = null;
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