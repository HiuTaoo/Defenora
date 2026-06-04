using _Script.BT.Node;
using UnityEngine;

public class MonkMoveToSafeRangeNode : BTActionNode
{
    private readonly Monk monk;
    private bool _hasStartedMove;

    public MonkMoveToSafeRangeNode(Unit unit) : base(unit)
    {
        monk = unit as Monk;
    }

    public override BTStatus Tick()
    {
        var building = monk.assignedBuilding;
        var target = monk.monkBlackBoard.lowHPAlly;

        if (building == null || target == null)
        {
            ResetNode();
            return BTStatus.Failure;
        }

        if (!_hasStartedMove)
        {
            Vector2 buildingPos = building.transform.position;
            Vector2 allyPos = target.transform.position;

            var maxAllowedRange = building.range / 2f;

            var vectorToAlly = allyPos - buildingPos;
            var clampedVector = Vector2.ClampMagnitude(vectorToAlly, maxAllowedRange);
            var targetWorldPos = buildingPos + clampedVector;

            var targetGridCell = Vector3Int.FloorToInt(targetWorldPos);
            targetGridCell.z = 0;

            var startGridCell = Vector3Int.FloorToInt(monk.transform.position);
            startGridCell.z = 0;

            if (startGridCell == targetGridCell)
            {
                ResetNode();
                return BTStatus.Success;
            }

            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                startGridCell, monk.floorAgent._currentFloorIndex,
                targetGridCell, building.layerIndex);

            if (path == null || !path.isValid)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            monk.MoveToTargetPosition(path);
            _hasStartedMove = true;
            monk.currentState = UnitState.Move;
            monk.animState = AnimState.Moving;
        }

        if (monk.characterMovement != null && monk.characterMovement.moving) return BTStatus.Running;

        ResetNode();
        return BTStatus.Success;
    }

    private void ResetNode()
    {
        _hasStartedMove = false;
        if (monk.characterMovement != null) monk.characterMovement.RequestStopMoving();
        monk.currentState = UnitState.Idle;
        monk.animState = AnimState.Idle;
    }
}