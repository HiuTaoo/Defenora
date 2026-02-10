namespace _Script.BT.Node.BuilderNode
{
    public class IdleNode : BTActionNode
    {
        public IdleNode(Builder builder) : base(builder) {}

        public override BTStatus Tick()
        {

            return BTStatus.Running;
        }
    }

}