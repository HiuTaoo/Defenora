namespace _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence
{
    public class FindPathToObstacleNode: BTActionNode
    {
        public FindPathToObstacleNode(Builder builder): base(builder){}

        public override BTStatus Tick()
        {
            if (builder.builderBlackBoard != null)
            {
                if (builder.builderBlackBoard.currentObstacle != null)
                {
                    if (builder.CheckPathToObstacleObject())
                    {
                        return BTStatus.Success;
                    }
                }
            }

            return BTStatus.Failure;
        }
    }
}