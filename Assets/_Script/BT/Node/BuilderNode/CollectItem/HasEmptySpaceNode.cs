using System.Linq;

namespace _Script.BT.Node.BuilderNode
{
    public class HasEmptySpaceNode : BTActionNode
    {
        public HasEmptySpaceNode(Builder builder) : base(builder) { }

        public override BTStatus Tick()
        {
            if (!builder.currentInventory.IsFull)
                return BTStatus.Success;
            return BTStatus.Failure;
        }
    }
}