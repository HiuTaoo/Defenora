using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class IsInSafetyRangeOfBuildingNode : BTNode
    {
        private readonly Archer archer;

        public IsInSafetyRangeOfBuildingNode(Archer archer)
        {
            this.archer = archer;
        }

        public override BTStatus Tick()
        {
            if (archer.assignedBuilding != null)
                return BTStatus.Success;
            if (archer.archerBlackBoard.nearestBuilding == null) return BTStatus.Success;

            var distance = Vector2.Distance(
                archer.transform.position,
                archer.archerBlackBoard.nearestBuilding.transform.position
            );

            var building = archer.archerBlackBoard.nearestBuilding.GetComponent<Building>();

            if (distance > building.range) return BTStatus.Success;

            return BTStatus.Failure;
        }
    }
}