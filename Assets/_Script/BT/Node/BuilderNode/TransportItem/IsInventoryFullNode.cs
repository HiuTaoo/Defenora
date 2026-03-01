namespace _Script.BT.Node.BuilderNode
{
    public class IsInventoryFullNode: BTActionNode
    {
        public IsInventoryFullNode(Builder builder):base(builder){}
        public override BTStatus Tick()
        {
            if(builder.currentInventory.IsFull)
                return BTStatus.Success;
            return BTStatus.Failure;
        }
    }
}