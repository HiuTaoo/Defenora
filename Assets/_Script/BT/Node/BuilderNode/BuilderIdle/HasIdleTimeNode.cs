namespace _Script.BT.Node.BuilderNode.Idle
{
    public class HasIdleTimeNode : BTActionNode
    {
        public HasIdleTimeNode(Unit unit) : base(unit)
        {
        }

        public override BTStatus Tick()
        {
            if (unit.currentState == UnitState.Idle) return BTStatus.Success;

            return BTStatus.Failure;
        }
    }
}