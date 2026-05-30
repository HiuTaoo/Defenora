using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class ArcherWanderActionNode : BTActionNode
    {
        private const float WANDER_RADIUS = 3f;
        private readonly Archer archer;
        private bool hasStartedMove;
        private Vector3Int wanderTarget;

        public ArcherWanderActionNode(Unit unit) : base(unit)
        {
            // Ép kiểu chuẩn về Archer giống như cách làm với Builder
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if (!hasStartedMove)
            {
                // Lấy ngay vị trí HIỆN TẠI của Archer làm tâm để chọn điểm ngẫu nhiên
                wanderTarget = GetRandomGridPointAround(
                    Vector3Int.RoundToInt(archer.transform.position),
                    WANDER_RADIUS
                );

                var node = GraphNode.Instance.GetNode(wanderTarget, archer.characterMovement.CurrentLayer);
                if (node == null || !node.isWalkable)
                    return BTStatus.Failure;

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

        public override void ClearState()
        {
            base.ClearState();
            hasStartedMove = false;
        }

        private void FinishMove()
        {
            hasStartedMove = false;
            if (archer != null) archer.animState = AnimState.Idle;
        }
    }
}