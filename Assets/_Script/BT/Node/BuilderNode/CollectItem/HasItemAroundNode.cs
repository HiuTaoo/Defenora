namespace _Script.BT.Node.BuilderNode
{
    public class HasItemAroundNode: BTActionNode
    {
        private Builder builder;

        public HasItemAroundNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }
        public override BTStatus Tick()
        {
            if (builder.FindItemAround() != null)
            {
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}