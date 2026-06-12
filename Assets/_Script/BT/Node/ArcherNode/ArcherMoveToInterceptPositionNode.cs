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

        if (archer.archerBlackBoard.detectedEnemy != null || archer.currentTarget != null)
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
                ClearAlarmState();
                return BTStatus.Failure;
            }

            Vector2 buildingPos = building.transform.position;
            var enemyPos = archer.lastSeenPosition; 
            var guardRadius = building.range;
            var vectorToEnemy = enemyPos - buildingPos;

            if (vectorToEnemy.sqrMagnitude < 0.001f) vectorToEnemy = Vector2.up;

            var targetPos = buildingPos + vectorToEnemy.normalized * Mathf.Min(vectorToEnemy.magnitude, guardRadius);
            var targetLayer = building.layerIndex;
            var baseTargetCell = Vector3Int.FloorToInt(targetPos);
            baseTargetCell.z = 0;

            var startCell = Vector3Int.FloorToInt(archer.transform.position);
            startCell.z = 0;

            if (startCell == baseTargetCell)
            {
                ClearAlarmState(); 
                return BTStatus.Success;
            }

            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(startCell, archer.floorAgent._currentFloorIndex,
                baseTargetCell, targetLayer);
            if (path == null || !path.isValid)
            {
                ClearAlarmState();
                return BTStatus.Failure;
            }

            archer.MoveToTargetPosition(path);
            hasStartedMove = true;
            archer.currentState = UnitState.Move;
            return BTStatus.Running;
        }

        if (archer.characterMovement != null && archer.characterMovement.moving)
            return BTStatus.Running;

        if (hasStartedMove && !archer.characterMovement.moving)
        {
            ClearAlarmState();
            ResetNode();
            return BTStatus.Success;
        }

        return BTStatus.Running;
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