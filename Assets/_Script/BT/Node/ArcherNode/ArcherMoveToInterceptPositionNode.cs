using _Script.BT.Node;
using UnityEngine;

public class ArcherMoveToInterceptPositionNode : BTActionNode
{
    private readonly Archer archer;
    private bool hasStartedMove;

    public ArcherMoveToInterceptPositionNode(Unit unit) : base(unit)
    {
        archer = unit as Archer;
    }

    public override BTStatus Tick()
    {
        if (archer == null || archer.lastSeenPosition == Vector2.zero)
        {
            ResetNode();
            return BTStatus.Failure;
        }

        if (archer.archerBlackBoard.detectedEnemy != null &&
            Vector2.Distance(archer.transform.position, archer.archerBlackBoard.detectedEnemy.transform.position) <=
            archer.attackRange)
        {
            ResetNode();
            return BTStatus.Success;
        }

        if (!hasStartedMove)
        {
            var building = archer.assignedBuilding;

            if (building == null) building = archer.FindNearestBuilding(archer.transform.position);

            if (building == null)
            {
                Debug.LogWarning(
                    $"[{archer.gameObject.name}] ❌ Không tìm thấy bất kỳ công trình nào kề cạnh để làm điểm neo phòng thủ. Hủy đánh chặn!");
                ClearAlarmState();
                return BTStatus.Failure;
            }

            Vector2 buildingPos = building.transform.position;
            var enemyPos = archer.lastSeenPosition;

            var guardRadius = building.range;

            var vectorToEnemy = enemyPos - buildingPos;

            if (vectorToEnemy.sqrMagnitude < 0.001f) vectorToEnemy = Vector2.up;

            var currentDistance = vectorToEnemy.magnitude;
            var finalClampDistance = Mathf.Min(currentDistance, guardRadius);

            var targetPos = buildingPos + vectorToEnemy.normalized * finalClampDistance;

            // -----------------------------------------------l-------------------------
            var targetLayer = building.layerIndex;
            var baseTargetCell = Vector3Int.FloorToInt(targetPos);
            baseTargetCell.z = 0;

            var finalTargetCell = baseTargetCell;
            var foundWalkableCell = false;

            var baseNode = GraphNode.Instance.GetNode(baseTargetCell, targetLayer);
            if (baseNode != null && baseNode.isWalkable)
            {
                foundWalkableCell = true;
            }
            else
            {
                const int searchRadius = 1;
                var minDistanceToIdeal = float.MaxValue;

                for (var x = -searchRadius; x <= searchRadius; x++)
                for (var y = -searchRadius; y <= searchRadius; y++)
                {
                    var checkCell = baseTargetCell + new Vector3Int(x, y, 0);

                    var buildingCell = Vector3Int.FloorToInt(buildingPos);
                    if (Mathf.Abs(checkCell.x - buildingCell.x) > guardRadius + 1 ||
                        Mathf.Abs(checkCell.y - buildingCell.y) > guardRadius + 1)
                        continue;

                    var node = GraphNode.Instance.GetNode(checkCell, targetLayer);
                    if (node != null && node.isWalkable)
                    {
                        var dist = Vector2.Distance(targetPos, new Vector2(checkCell.x + 0.5f, checkCell.y + 0.5f));
                        if (dist < minDistanceToIdeal)
                        {
                            minDistanceToIdeal = dist;
                            finalTargetCell = checkCell;
                            foundWalkableCell = true;
                        }
                    }
                }
            }

            if (!foundWalkableCell)
            {
                Debug.LogWarning(
                    $"[{archer.gameObject.name}] ❌ Vùng rìa gác {baseTargetCell} của nhà {building.gameObject.name} bị bịt kín. Hủy lệnh!");
                ClearAlarmState();
                return BTStatus.Failure;
            }

            var startCell = Vector3Int.FloorToInt(archer.transform.position);
            startCell.z = 0;

            if (startCell == finalTargetCell)
            {
                ResetNode();
                return BTStatus.Success;
            }

            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                startCell,
                archer.floorAgent._currentFloorIndex,
                finalTargetCell,
                targetLayer);

            if (path == null || !path.isValid)
            {
                Debug.LogWarning(
                    $"[{archer.gameObject.name}] ❌ Không tìm được đường A* đến ô cứu kẹt rìa nhà {finalTargetCell}. Hủy!");
                ClearAlarmState();
                return BTStatus.Failure;
            }

            archer.MoveToTargetPosition(path);

            hasStartedMove = true;
            archer.currentState = UnitState.Move;
            archer.animState = AnimState.Moving;

            return BTStatus.Running;
        }

        if (archer.characterMovement != null && archer.characterMovement.moving) return BTStatus.Running;

        if (hasStartedMove && archer.archerBlackBoard.detectedEnemy == null)
        {
            Debug.Log(
                $"[{archer.gameObject.name}] 🛡️ Đã hành quân đến vị trí chặn đầu thành công nhưng thực tế trống trải. Dọn dẹp!");
            ClearAlarmState();
        }

        ResetNode();
        return BTStatus.Success;
    }

    private void ClearAlarmState()
    {
        archer.ClearAggro();
        archer.isAlerted = false;
        archer.aggroTimer = 0f;
        archer.GetBT()?.ClearState();
        ResetNode();
    }

    private void ResetNode()
    {
        hasStartedMove = false;
        if (archer.characterMovement != null) archer.characterMovement.RequestStopMoving();
        archer.currentState = UnitState.Idle;
        archer.animState = AnimState.Idle;
    }
}