using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence
{
    public class HasObstacleNode : BTActionNode
    {
        private Builder builder;

        public HasObstacleNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentTask == null || builder.currentTask.targetGameObject == null)
                return BTStatus.Failure;

            var building = builder.currentTask.targetGameObject.GetComponent<Building>();
            if (building == null)
            {
                Debug.Log("Building null");
                return BTStatus.Failure;
            }
  
            var obstacle = building.FindObstacleObject();
            if (obstacle == null)
                return BTStatus.Failure; 

            if (!obstacle.IsClaimed && obstacle.TryClaim(builder))
            {
                builder.builderBlackBoard.currentObstacle = obstacle;
                builder.targetGO = (obstacle as Component)?.gameObject;
            }

            return BTStatus.Success;
        }

    }
}