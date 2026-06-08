using _Script.BT.Node;
using _Script.Enum;
using UnityEngine;

public class EnemyAttackBuildingNode : BTActionNode
{
    public EnemyAttackBuildingNode(Unit unit) : base(unit)
    {
    }

    public override BTStatus Tick()
    {
        if (unit.isKnockedBack)
        {
            ResetState();
            return BTStatus.Failure;
        }

        if (unit.currentTarget == null)
        {
            ResetState();
            return BTStatus.Failure;
        }

        var building = unit.currentTarget.GetComponent<Building>();
        if (building == null || building.buildingState == BuildingState.Destroyed)
        {
            ResetState();
            return BTStatus.Success;
        }

        if (unit.isAttacking) return BTStatus.Running;

        if (Time.time >= unit.lastAttackTime + unit.attackCooldown)
        {
            unit.lastAttackTime = Time.time;
            unit.StartAttackSignal();

            unit.currentState = UnitState.Attack;
            unit.animState = AnimState.Attacking;
        }
        else
        {
            unit.currentState = UnitState.Idle;
            unit.animState = AnimState.Idle;
        }

        return BTStatus.Running;
    }

    private void ResetState()
    {
        unit.EndAttackSignal();
        unit.currentState = UnitState.Idle;
        unit.animState = AnimState.Idle;
    }
}