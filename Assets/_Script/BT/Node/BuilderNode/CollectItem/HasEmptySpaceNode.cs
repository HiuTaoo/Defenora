using System.Linq;

namespace _Script.BT.Node.BuilderNode
{
    public class HasEmptySpaceNode : BTActionNode
    {
        private Builder builder;

        public HasEmptySpaceNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (!builder.currentInventory.IsFull)
                return BTStatus.Success;
            return BTStatus.Failure;
        }
    }
}