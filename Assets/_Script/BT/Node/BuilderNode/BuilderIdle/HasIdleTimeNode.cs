using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Idle
{
    public class HasIdleTimeNode: BTActionNode
    {
        private Builder builder;

        public HasIdleTimeNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }
        public override BTStatus Tick()
        {
            if (builder.currentState == UnitState.Idle)
            {
                return BTStatus.Success;
            }
               
            return BTStatus.Failure;
        }
    }
}