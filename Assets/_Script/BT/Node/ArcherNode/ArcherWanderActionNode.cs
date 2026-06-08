using System.Collections.Generic;
using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class ArcherWanderActionNode : BTActionNode
    {
        private const int WANDER_RADIUS = 3; 
        private readonly Archer archer;
        private bool hasStartedMove;
        private Vector3Int wanderTarget;

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
                if (TryFindWanderTargetBFS(Vector3Int.RoundToInt(archer.transform.position), out wanderTarget))
                {
                    archer.animState = AnimState.Moving;

                    var path = archer.FindBestPathToTarget(archer.gameObject, archer.characterMovement.CurrentLayer);

                    archer.characterMovement.MoveToPosition(wanderTarget, archer.characterMovement.CurrentLayer);

                    hasStartedMove = true;
                    return BTStatus.Running;
                }

                return BTStatus.Failure;
            }

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

            var queue = new Queue<Vector3Int>();
            var visited = new HashSet<Vector3Int>();
            var validCandidates = new List<Vector3Int>();

            queue.Enqueue(startGridPos);
            visited.Add(startGridPos);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                var distanceX = Mathf.Abs(current.x - startGridPos.x);
                var distanceY = Mathf.Abs(current.y - startGridPos.y);
                if (distanceX > WANDER_RADIUS || distanceY > WANDER_RADIUS)
                    continue;

                if (current != startGridPos)
                {
                    var node = GraphNode.Instance.GetNode(current, currentLayer);
                    if (node != null && node.isWalkable) validCandidates.Add(current);
                }

                foreach (var dir in Directions)
                {
                    var neighbor = current + dir;

                    if (!visited.Contains(neighbor))
                        if (Mathf.Abs(neighbor.x - startGridPos.x) <= WANDER_RADIUS &&
                            Mathf.Abs(neighbor.y - startGridPos.y) <= WANDER_RADIUS)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                }
            }

            if (validCandidates.Count > 0)
            {
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