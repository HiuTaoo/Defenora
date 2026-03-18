using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Idle
{
    public class MoveFollowAvaiablePathNode : BTActionNode
    {
        private Builder builder;
        public MoveFollowAvaiablePathNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        private bool hasStartedMove = false;
        private Vector3Int wanderTarget;

        private const float WANDER_RADIUS = 3f;

        public override BTStatus Tick()
        {
            if (!hasStartedMove)
            {
                wanderTarget = GetRandomGridPointAround(
                    Vector3Int.RoundToInt(builder.transform.position),
                    WANDER_RADIUS
                );

                var node = GraphNode.Instance.GetNode(wanderTarget, builder.characterMovement.CurrentLayer);
                if (node == null || !node.isWalkable)
                    return BTStatus.Failure;
                
                builder.UpdateAnim();
                builder.animFSM.ChangeState(UnitState.Moving);

                builder.characterMovement.MoveToPosition(wanderTarget, builder.characterMovement.CurrentLayer); 

                hasStartedMove = true;
                return BTStatus.Running;
            }

            if (builder.characterMovement.moving)
                return BTStatus.Running;

            FinishMove();
            return BTStatus.Success;
        }

        private Vector3Int GetRandomGridPointAround(Vector3Int center, float radius)
        {
            Vector2 random2D = Random.insideUnitCircle * radius;

            Vector3 randomWorld = new Vector3(
                center.x + random2D.x,
                center.y,
                center.z + random2D.y
            );

            return Vector3Int.RoundToInt(randomWorld);
        }

        private void ResetNode()
        {
            hasStartedMove = false;
        }

        private void FinishMove()
        {
            ResetNode();
            builder.animFSM.ChangeState(UnitState.Idle);
        }
    }
}