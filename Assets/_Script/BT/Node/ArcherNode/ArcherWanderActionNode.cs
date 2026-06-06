using System.Collections.Generic;
using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class ArcherWanderActionNode : BTActionNode
    {
        private const int WANDER_RADIUS = 3; // Đổi sang số nguyên int để chạy BFS trên lưới Grid chuẩn
        private readonly Archer archer;
        private bool hasStartedMove;
        private Vector3Int wanderTarget;

        // Hướng di chuyển 4 bên (Đông, Tây, Nam, Bắc) để phục vụ việc loang BFS
        private static readonly Vector3Int[] Directions =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        public ArcherWanderActionNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if (!hasStartedMove)
            {
                // Thay thế hàm lấy điểm Random cũ bằng thuật toán BFS loang rộng
                if (TryFindWanderTargetBFS(Vector3Int.RoundToInt(archer.transform.position), out wanderTarget))
                {
                    archer.animState = AnimState.Moving;

                    // Sử dụng hàm di chuyển qua Pathfinding của Unit thay vì MoveToPosition thô 
                    // để Archer mò đường chuẩn A* tránh vật cản tới ô BFS vừa tìm
                    var path = archer.FindBestPathToTarget(archer.gameObject, archer.characterMovement.CurrentLayer);

                    // Gọi hàm kích hoạt di chuyển có sẵn trong Unit.cs của bạn
                    archer.characterMovement.MoveToPosition(wanderTarget, archer.characterMovement.CurrentLayer);

                    hasStartedMove = true;
                    return BTStatus.Running;
                }

                // Nếu quét hết bán kính BFS mà xung quanh bị bít lối hoàn toàn ➔ Báo Failure để cây BT chuyển hướng khác
                return BTStatus.Failure;
            }

            // Nếu đang trong quá trình di chuyển di động, giữ trạng thái Running
            if (archer.characterMovement.moving)
                return BTStatus.Running;

            FinishMove();
            return BTStatus.Success;
        }

        /// <summary>
        ///     Thuật toán BFS quét loang rộng tìm kiếm ô đất trống đi lại được xung quanh Archer
        /// </summary>
        private bool TryFindWanderTargetBFS(Vector3Int startGridPos, out Vector3Int resultTarget)
        {
            resultTarget = startGridPos;
            var currentLayer = archer.characterMovement.CurrentLayer;

            if (GraphNode.Instance == null) return false;

            // Các cấu trúc dữ liệu cơ bản của BFS
            var queue = new Queue<Vector3Int>();
            var visited = new HashSet<Vector3Int>();
            var validCandidates = new List<Vector3Int>();

            queue.Enqueue(startGridPos);
            visited.Add(startGridPos);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Kiểm tra điều kiện khoảng cách Manhattan: Không cho phép loang vượt quá bán kính quy định
                var distanceX = Mathf.Abs(current.x - startGridPos.x);
                var distanceY = Mathf.Abs(current.y - startGridPos.y);
                if (distanceX > WANDER_RADIUS || distanceY > WANDER_RADIUS)
                    continue;

                // Nếu ô hiện tại không phải là ô gốc đứng ban đầu và đi lại được ➔ Gom vào danh sách ứng viên sáng giá
                if (current != startGridPos)
                {
                    var node = GraphNode.Instance.GetNode(current, currentLayer);
                    if (node != null && node.isWalkable) validCandidates.Add(current);
                }

                // Loang tiếp sang 4 ô hàng xóm xung quanh
                foreach (var dir in Directions)
                {
                    var neighbor = current + dir;

                    if (!visited.Contains(neighbor))
                        // Kiểm tra biên khoảng cách trước khi nhét vào Queue để tối ưu bộ nhớ
                        if (Mathf.Abs(neighbor.x - startGridPos.x) <= WANDER_RADIUS &&
                            Mathf.Abs(neighbor.y - startGridPos.y) <= WANDER_RADIUS)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                }
            }

            // Nếu tìm thấy các ô trống hợp lệ trong bán kính BFS
            if (validCandidates.Count > 0)
            {
                // Bốc ngẫu nhiên 1 trong các ô đi lại được đã tìm thấy để hành vi Wander nhìn tự nhiên hơn
                var randomIndex = Random.Range(0, validCandidates.Count);
                resultTarget = validCandidates[randomIndex];
                return true;
            }

            return false;
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