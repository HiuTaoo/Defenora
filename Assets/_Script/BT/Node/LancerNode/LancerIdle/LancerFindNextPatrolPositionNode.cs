using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIdle
{
    public class LancerFindNextPatrolPositionNode : BTActionNode
    {
        private Lancer lancer;

        public LancerFindNextPatrolPositionNode(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if (lancer == null) return BTStatus.Failure;

            var targetBuilding = lancer.assignedBuilding;

            if (targetBuilding == null)
            {
                targetBuilding = lancer.FindRandomBuilding(lancer.transform.position);

                if (targetBuilding == null) return BTStatus.Failure;
            }

            var nextPosition = lancer.FindPatrolPosition(
                Vector3Int.FloorToInt(targetBuilding.transform.position),
                lancer.minRadius,
                lancer.maxRadius,
                targetBuilding.layerIndex
            );
            
            if (nextPosition == Vector3Int.zero)
                return BTStatus.Failure;

            lancer.lancerBlackBoard.patrolTarget = nextPosition;

            var startPosition = lancer.transform.position;
            var worldPosition = Vector3Int.FloorToInt(startPosition);
            worldPosition.z = 0;

            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                worldPosition, 
                lancer.floorAgent._currentFloorIndex,
                nextPosition,
                targetBuilding.layerIndex
            );

            if (path == null || !path.isValid)
                return BTStatus.Failure;

            lancer.lancerBlackBoard.pathFinding = path;
            return BTStatus.Success;
        }
    }
}