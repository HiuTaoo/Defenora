using _Script.BT.Node;
using UnityEngine;

public class IsOutsideBuildingRangeConditionNode : BTActionNode
{
    private readonly Archer archer;

    public IsOutsideBuildingRangeConditionNode(Unit unit) : base(unit)
    {
        archer = (Archer)unit;
    }

    public override BTStatus Tick()
    {
        if (archer.assignedBuilding == null) return BTStatus.Failure;

        var distance = Vector2.Distance(archer.transform.position, archer.assignedBuilding.transform.position);

        var guardRadius = archer.assignedBuilding.range;

        if (distance > guardRadius + 0.2f) return BTStatus.Success;

        return BTStatus.Failure;
    }
}