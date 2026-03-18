namespace _Script.BT.Node.BuilderNode
{
    public class CheckPathToAdjacentTargetNode : BTActionNode
    {
        private Builder builder;
        public CheckPathToAdjacentTargetNode(Unit unit) : base(unit)
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
                    builder.FindBestPathToAnyAdjacent(builder.currentTask);
            }

            return builder.builderBlackBoard.pathFinding != null
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}