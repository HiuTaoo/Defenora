namespace _Script.BT.Node.BuilderNode
{
    public class IdleNode : BTActionNode
    {
        
        public IdleNode(Unit unit) : base(unit) {}

        public override BTStatus Tick()
        {
            return BTStatus.Running;
        }
    }

}