using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence
{
    public class FindPathToObstacleNode: BTActionNode
    {
        private Builder builder;
        public FindPathToObstacleNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

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
            Debug.Log("Can't find path to obstacle");
            return BTStatus.Failure;
        }
    }
}