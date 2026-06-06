using _Script.Unit_Management_System.UnitScript;
using UnityEngine;

namespace _Script.BT.Node.CivilianNode
{
    public class CivilianMoveAroundNode : BTActionNode
    {
        public Civilian civilian;

        public CivilianMoveAroundNode(Unit unit) : base(unit)
        {
            civilian = unit as Civilian;
        }

        private bool hasStartedMove;
        private Vector3Int wanderTarget;

        private const float WANDER_RADIUS = 3f;

        public override BTStatus Tick()
        {
            if (!hasStartedMove)
            {
                wanderTarget = GetRandomGridPointAround(
                    Vector3Int.RoundToInt(civilian.transform.position),
                    WANDER_RADIUS
                );

                var node = GraphNode.Instance.GetNode(wanderTarget, civilian.characterMovement.CurrentLayer);
                if (node == null || !node.isWalkable)
                    return BTStatus.Failure;

                civilian.animState = AnimState.Moving;
                civilian.characterMovement.MoveToPosition(wanderTarget, civilian.characterMovement.CurrentLayer);

                hasStartedMove = true;
                return BTStatus.Running;
            }

            if (civilian.characterMovement.moving)
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
            civilian.animState = AnimState.Idle;
        }

        private void FinishMove()
        {
            ResetNode();
        }
    }
}