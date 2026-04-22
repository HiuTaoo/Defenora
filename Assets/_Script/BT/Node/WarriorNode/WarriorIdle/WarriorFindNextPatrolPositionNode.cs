using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorIdle
{
    public class WarriorFindNextPatrolPositionNode: BTActionNode
    {
        private Warrior warrior;

        public WarriorFindNextPatrolPositionNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (warrior.assignedBuilding == null)
            {
                return BTStatus.Failure;
            }
            
            var nextPosition = warrior.FindPatrolPosition(
                    Vector3Int.FloorToInt(warrior.assignedBuilding.transform.position)
                    , 1, 2);
            
            if (nextPosition == Vector3Int.zero)
                return BTStatus.Failure;

            warrior.warriorBlackBoard.patrolTarget = nextPosition;
            var startPosition = warrior.transform.transform.position;
            var worldPosition = Vector3Int.FloorToInt(startPosition);
            worldPosition.z = 0;
            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                worldPosition, 
                warrior.floorAgent._currentFloorIndex,
                nextPosition,
                warrior.assignedBuilding.layerIndex);
            
            if(!path.isValid)
                return BTStatus.Failure;

            warrior.warriorBlackBoard.pathFinding = path;
            return BTStatus.Success;
        }
    }
}