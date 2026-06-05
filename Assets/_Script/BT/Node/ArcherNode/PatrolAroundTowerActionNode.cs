using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class PatrolAroundTowerActionNode : BTActionNode
    {
        private readonly Archer archer;
        private bool hasStartedMove;
        private Vector3Int wanderTarget;

        public PatrolAroundTowerActionNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if (!hasStartedMove)
            {
                Vector3 centerPosition;

                if (archer.assignedBuilding != null)
                    centerPosition = archer.assignedBuilding.transform.position;
                else if (archer.archerBlackBoard.nearestBuilding != null)
                    centerPosition = archer.archerBlackBoard.nearestBuilding.transform.position;
                else
                    return BTStatus.Failure;

                wanderTarget = GetRandomGridPointAround(
                    Vector3Int.RoundToInt(centerPosition),
                    archer.assignedBuilding.range
                );

                var node = GraphNode.Instance.GetNode(wanderTarget, archer.characterMovement.CurrentLayer);
                if (node == null || !node.isWalkable) return BTStatus.Failure;

                archer.animState = AnimState.Moving;
                archer.characterMovement.MoveToPosition(wanderTarget, archer.characterMovement.CurrentLayer);

                hasStartedMove = true;
                return BTStatus.Running;
            }

            if (archer.characterMovement.moving)
                return BTStatus.Running;

            FinishMove();
            return BTStatus.Success;
        }

        private Vector3Int GetRandomGridPointAround(Vector3Int center, float radius)
        {
            var random2D = Random.insideUnitCircle * radius;
            var randomWorld = new Vector3(
                center.x + random2D.x,
                center.y,
                center.z + random2D.y
            );
            return Vector3Int.RoundToInt(randomWorld);
        }
        
        private void ResetNode()
        {
            hasStartedMove = false;
            if (archer != null) archer.animState = AnimState.Idle;
        }

        private void FinishMove()
        {
            ResetNode();
        }
    }
}