namespace _Script.BT
{
    public class InvertNode : BTNode
    {
        private readonly BTNode child;

        public InvertNode(BTNode child)
        {
            this.child = child;
        }

        public override BTStatus Tick()
        {
            var status = child.Tick();

            if (status == BTStatus.Success)
            {
                return BTStatus.Failure;
            }
            
            if (status == BTStatus.Failure)
            {
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }
    }
}