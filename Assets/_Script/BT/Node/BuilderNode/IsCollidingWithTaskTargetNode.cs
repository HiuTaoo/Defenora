namespace _Script.BT.Node.BuilderNode
{
    public class IsCollidingWithTaskTargetNode:BTActionNode
    {
        private Builder builder;

        public IsCollidingWithTaskTargetNode(Unit unit) : base(unit)
        {
            builder = unit as Builder;
        }

        public override BTStatus Tick()
        {
            if (builder != null && builder.IsCollidingWithTaskTarget())
            {
                return BTStatus.Success; 
            }
            
            return BTStatus.Failure;
        }
    }
}