namespace _Script.BT.Node.BuilderNode
{
    public class CheckPathToTargetNode : BTActionNode
    {
        public CheckPathToTargetNode(Builder builder) : base(builder) {}

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