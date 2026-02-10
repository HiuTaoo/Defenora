namespace _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence
{
    public class RepeatUntilFailureNode : BTNode
    {
        private readonly BTNode child;

        public RepeatUntilFailureNode(BTNode child)
        {
            this.child = child;
        }

        public override BTStatus Tick()
        {
            var status = child.Tick();

            if (status == BTStatus.Failure)
            {
                return BTStatus.Success;
            }
            
            return BTStatus.Running;
        }
    }
}