using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class MoveToNearestBuildingActionNode : BTActionNode
    {
        private const float FIND_BUILDING_RADIUS = 30f;
        private const string BUILDING_TAG = "Building";
        private readonly Archer archer;
        private bool hasStartedMove;
        private Vector3 targetGridPos;

        public MoveToNearestBuildingActionNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if (!hasStartedMove)
            {
                if (archer.archerBlackBoard.nearestBuilding == null)
                    archer.archerBlackBoard.nearestBuilding = FindNearestBuilding();

                if (archer.archerBlackBoard.nearestBuilding == null) return BTStatus.Failure;

                var building = archer.archerBlackBoard.nearestBuilding.GetComponent<Building>();

                targetGridPos = building.GetRandomPositionAroundBuilding();
                var targetLayer = archer.archerBlackBoard.nearestBuilding.GetComponentInChildren<Building>()
                    .layerIndex;

                Debug.Log($"Move to building: {archer.archerBlackBoard.nearestBuilding.GetId()}");
                archer.animState = AnimState.Moving;
                archer.characterMovement.MoveToPosition(Vector3Int.FloorToInt(targetGridPos), targetLayer);

                hasStartedMove = true;
                return BTStatus.Running;
            }

            if (archer.characterMovement.moving)
                return BTStatus.Running;

            hasStartedMove = false;
            return BTStatus.Success;
        }

        private GameObject FindNearestBuilding()
        {
            var hits = Physics2D.OverlapCircleAll(archer.transform.position, FIND_BUILDING_RADIUS);
            GameObject nearest = null;
            var minDistance = Mathf.Infinity;

            foreach (var hit in hits)
                if (hit.CompareTag(BUILDING_TAG))
                {
                    var distance = Vector2.Distance(archer.transform.position, hit.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = hit.gameObject;
                    }
                }

            return nearest;
        }

        public override void ClearState()
        {
            base.ClearState();
            hasStartedMove = false;
        }
    }
}