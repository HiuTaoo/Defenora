using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class MoveToNearestBuildingActionNode : BTActionNode
    {
        private const float FIND_BUILDING_RADIUS = 30f;
        private const string BUILDING_TAG = "Building";
        private readonly Archer archer;
        private bool hasStartedMove;
        private Vector3Int targetGridPos;

        public MoveToNearestBuildingActionNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if (!hasStartedMove)
            {
                // Chỉ quét tìm kiếm nếu trong Blackboard chưa lưu công trình nào
                if (archer.archerBlackBoard.nearestBuilding == null)
                    archer.archerBlackBoard.nearestBuilding = FindNearestBuilding();

                // Nếu xung quanh hoàn toàn không có công trình nào
                if (archer.archerBlackBoard.nearestBuilding == null) return BTStatus.Failure;

                // Lấy vị trí Grid của tòa nhà đã tìm thấy
                var buildingPos = archer.archerBlackBoard.nearestBuilding.transform.position;
                targetGridPos = Vector3Int.RoundToInt(buildingPos);

                // Kiểm tra tính hợp lệ (Nếu đứng đè lên tâm building ko đi được, có thể tìm node walkable cạnh bên)
                var node = GraphNode.Instance.GetNode(targetGridPos, archer.characterMovement.CurrentLayer);
                if (node == null || !node.isWalkable)
                {
                    // Tìm một vị trí ngẫu nhiên sát sạt building (bán kính nhỏ 1 đơn vị) để đứng
                    var miniRandom = Random.insideUnitCircle * 1f;
                    targetGridPos = Vector3Int.RoundToInt(buildingPos + new Vector3(miniRandom.x, 0, miniRandom.y));
                }

                // Thực hiện di chuyển về nhà
                archer.animState = AnimState.Moving;
                archer.characterMovement.MoveToPosition(targetGridPos, archer.characterMovement.CurrentLayer);

                hasStartedMove = true;
                return BTStatus.Running;
            }

            // Đang trên đường di chuyển về công trình gần nhất
            if (archer.characterMovement.moving)
                return BTStatus.Running;

            // Đã đến nơi an toàn
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