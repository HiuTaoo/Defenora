using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIdle
{
    public class FindNextPatrolPositionNode: BTActionNode
    {
        private Lancer lancer;
        public FindNextPatrolPositionNode(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            var nextPosition = lancer.FindPatrolPosition(
                Vector3Int.FloorToInt(lancer.assignedBuilding.transform.position)
                , 1, 2);
            
            if (nextPosition == Vector3Int.zero)
                return BTStatus.Failure;

            lancer.lancerBlackBoard.patrolTarget = nextPosition;
            var startPosition = lancer.transform.transform.position;
            var worldPosition = Vector3Int.FloorToInt(startPosition);
            worldPosition.z = 0;
            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                worldPosition, 
                lancer.floorAgent._currentFloorIndex,
                nextPosition,
                lancer.assignedBuilding.layerIndex);
            
            if(!path.isValid)
                return BTStatus.Failure;

            lancer.lancerBlackBoard.pathFinding = path;
            return BTStatus.Success;
        }
    }
}