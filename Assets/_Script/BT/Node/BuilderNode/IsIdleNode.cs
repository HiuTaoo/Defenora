namespace _Script.BT.Node.BuilderNode
{
    public class IsIdleNode: BTActionNode
    {
        public IsIdleNode(Builder builder) : base(builder) { }
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