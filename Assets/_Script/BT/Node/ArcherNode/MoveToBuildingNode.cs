using _Script.BT.Node;
using UnityEngine;

public class MoveToBuildingNode : BTActionNode
{
    private readonly Archer archer;
    private bool hasStartedMove;
    private Vector3 targetGridPos;
    private int targetLayer;

    public MoveToBuildingNode(Unit unit) : base(unit)
    {
        archer = (Archer)unit;
    }

    public override BTStatus Tick()
    {
        if (archer.assignedBuilding == null)
        {
            Debug.LogWarning($"[{archer.gameObject.name}] ❌ Gọi Node về nhà gán nhưng assignedBuilding đang bị NULL!");
            ResetMoveState();
            return BTStatus.Failure;
        }

        if (hasStartedMove && archer.characterMovement.moving)
        {
            archer.currentState = UnitState.Move;
            archer.animState = AnimState.Moving;
            return BTStatus.Running;
        }

        if (!hasStartedMove)
        {
            var building = archer.assignedBuilding;

            var currentDistance = Vector2.Distance(archer.transform.position, building.transform.position);

            if (currentDistance <= building.range)
            {
                ResetMoveState();
                return BTStatus.Success;
            }

            targetGridPos = building.GetRandomPositionAroundBuilding();
            targetLayer = building.layerIndex;

            archer.currentState = UnitState.Move;
            archer.animState = AnimState.Moving;
            archer.characterMovement.MoveToPosition(Vector3Int.FloorToInt(targetGridPos), targetLayer);

            hasStartedMove = true;
            return BTStatus.Running;
        }

        if (!archer.characterMovement.moving)
        {
            ResetMoveState();
            return BTStatus.Success;
        }

        return BTStatus.Running;
    }

    private void ResetMoveState()
    {
        hasStartedMove = false;
        archer.currentState = UnitState.Idle;
        archer.animState = AnimState.Idle;
    }
}