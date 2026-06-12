using UnityEngine;

namespace _Script.BT.Node.MonkNode.MonkIdle
{
    public class FindNextPatrolPositionMonkNode : BTActionNode
    {
        private Monk monk;

        public FindNextPatrolPositionMonkNode(Unit unit) : base(unit)
        {
            monk = unit as Monk;
        }

        public override BTStatus Tick()
        {
            if (monk == null) return BTStatus.Failure;

            var targetBuilding = monk.assignedBuilding;
            if (targetBuilding == null)
            {
                targetBuilding = monk.FindRandomBuilding(monk.transform.position);

                if (targetBuilding == null) return BTStatus.Failure;
            }

            var nextPosition = monk.FindPatrolPosition(
                Vector3Int.FloorToInt(targetBuilding.transform.position),
                1, 2,
                targetBuilding.layerIndex
            ); 
            
            if (nextPosition == Vector3Int.zero)
                return BTStatus.Failure;

            monk.monkBlackBoard.patrolTarget = nextPosition;

            var startPosition = monk.transform.position;
            var worldPosition = Vector3Int.FloorToInt(startPosition);
            worldPosition.z = 0;

            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                worldPosition, 
                monk.floorAgent._currentFloorIndex,
                nextPosition,
                targetBuilding.layerIndex
            );

            if (path == null || !path.isValid)
                return BTStatus.Failure;

            monk.monkBlackBoard.pathFinding = path;
            return BTStatus.Success;
        }
    }
}