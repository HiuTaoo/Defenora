using _Script.BT.Node;
using UnityEngine;

public class MonkMoveToSafeRangeNode : BTActionNode
{
    private readonly Monk monk;
    private bool _hasStartedMove;
    private Vector3Int _targetGridCell;

    public MonkMoveToSafeRangeNode(Unit unit) : base(unit)
    {
        monk = unit as Monk;
    }
    public override BTStatus Tick()
    {
        var building = monk.assignedBuilding;

        if (building == null || !monk.isAlerted || monk.lastSeenPosition == Vector2.zero)
        {
            ResetNode();
            return BTStatus.Failure;
        }

        if (_hasStartedMove)
        {
            if (monk.characterMovement != null && monk.characterMovement.moving)
            {
                monk.currentState = UnitState.Move;
                monk.animState = AnimState.Moving;
                return BTStatus.Running;
            }

            Debug.Log($"[🧘 MONK FLEE] ✨ {monk.gameObject.name} đã chạy trốn đến vùng an toàn khuất hướng còi hú thành công!");
            
            ResetNode(); 
            return BTStatus.Success; 
        }

        if (!_hasStartedMove)
        {
            Vector2 buildingPos = building.transform.position;
            Vector2 alarmPos = monk.lastSeenPosition;
            Vector2 monkPos = monk.transform.position;

            Vector2 fleeDirection = (monkPos - alarmPos).normalized;
            if (fleeDirection.sqrMagnitude < 0.001f) fleeDirection = Vector2.up;

            float escapeDistance = building.range * 0.6f;
            Vector2 targetWorldPos = buildingPos + fleeDirection * escapeDistance;

            _targetGridCell = Vector3Int.FloorToInt(targetWorldPos);
            _targetGridCell.z = 0;

            var startGridCell = Vector3Int.FloorToInt(monk.transform.position);
            startGridCell.z = 0;

            if (startGridCell == _targetGridCell)
            {
                monk.GetBT()?.ClearState();

                ResetNode();
                return BTStatus.Success;
            }

            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                startGridCell, monk.floorAgent._currentFloorIndex,
                _targetGridCell, building.layerIndex);

            if (path == null || !path.isValid)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            monk.MoveToTargetPosition(path);
            _hasStartedMove = true;
            monk.currentState = UnitState.Move;
            monk.animState = AnimState.Moving;
            return BTStatus.Running;
        }

        return BTStatus.Running;
    }

    private void ResetNode()
    {
        _hasStartedMove = false;
        if (monk.characterMovement != null) monk.characterMovement.RequestStopMoving();
        monk.currentState = UnitState.Idle;
        monk.animState = AnimState.Idle;
    }
}