namespace _Script.BT.Node.MonkNode.MonkIdle
{
    public class IsIdleMonkNode: BTActionNode
    {
        private Monk monk;

        public IsIdleMonkNode(Unit unit) : base(unit)
        {
            monk = unit as Monk;
        }

        public override BTStatus Tick()
        {
            if (monk.currentState == UnitState.Idle)
                return BTStatus.Success;
            return BTStatus.Failure;
        }
    }
}