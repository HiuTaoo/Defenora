namespace _Script.BT.Node.BuilderNode
{
    public class HasItemAroundNode: BTActionNode
    {
        public HasItemAroundNode(Builder builder):base(builder){}
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