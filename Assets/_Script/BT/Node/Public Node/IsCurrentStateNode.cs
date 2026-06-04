using _Script.BT.Node;

public class IsCurrentStateNode : BTActionNode
{
    private readonly UnitState targetState;

    public IsCurrentStateNode(Unit unit, UnitState stateToCheck) : base(unit)
    {
        targetState = stateToCheck;
    }

    public override BTStatus Tick()
    {
        if (unit == null) return BTStatus.Failure;

        if (unit.currentState == targetState) return BTStatus.Success;

        return BTStatus.Failure;
    }
}