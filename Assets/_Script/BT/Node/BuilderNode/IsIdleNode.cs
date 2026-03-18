namespace _Script.BT.Node.BuilderNode
{
    public class IsIdleNode: BTActionNode
    {
        private Builder builder;

        public IsIdleNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }
        public override BTStatus Tick()
        {
            if (builder.currentState == UnitState.Idle && (builder.currentTask == null || builder.currentTask.targetGameObject == null))
            {
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}