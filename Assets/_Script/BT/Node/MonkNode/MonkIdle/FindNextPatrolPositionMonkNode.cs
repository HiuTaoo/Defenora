using UnityEngine;

namespace _Script.BT.Node.MonkNode.MonkIdle
{
    public class FindNextPatrolPositionMonkNode: BTActionNode
    {
        private Monk monk;

        public FindNextPatrolPositionMonkNode(Unit unit) : base(unit)
        {
            monk = unit as Monk;
        }
        public override BTStatus Tick()
        {
            if(monk.assignedBuilding == null)
                return BTStatus.Failure;
            
            var nextPosition = monk.FindPatrolPosition(
                Vector3Int.FloorToInt(monk.assignedBuilding.transform.position)
                , 1, 2);
            
            if (nextPosition == Vector3Int.zero)
                return BTStatus.Failure;

            monk.monkBlackBoard.patrolTarget = nextPosition;
            var startPosition = monk.transform.transform.position;
            var worldPosition = Vector3Int.FloorToInt(startPosition);
            worldPosition.z = 0;
            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                worldPosition, 
                monk.floorAgent._currentFloorIndex,
                nextPosition,
                monk.assignedBuilding.layerIndex);
            
            if(!path.isValid)
                return BTStatus.Failure;

            monk.monkBlackBoard.pathFinding = path;
            return BTStatus.Success;
        }
    }
}