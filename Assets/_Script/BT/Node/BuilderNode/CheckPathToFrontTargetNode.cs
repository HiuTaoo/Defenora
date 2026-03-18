namespace _Script.BT.Node.BuilderNode
{
    public class CheckPathToFrontTargetNode: BTActionNode
    {
        private Builder builder;
        public CheckPathToFrontTargetNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }
        public override BTStatus Tick()
        {
            if (builder.currentTask == null)
                return BTStatus.Failure;

            if (builder.builderBlackBoard.pathFinding == null)
            {
                builder.builderBlackBoard.pathFinding =
                    builder.FindBestPathToFront(builder.currentTask);
            }

            return builder.builderBlackBoard.pathFinding != null
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}