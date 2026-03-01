using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Idle
{
    public class HasIdleTimeNode: BTActionNode
    {
        public HasIdleTimeNode(Builder builder): base(builder){}
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