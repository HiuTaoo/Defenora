namespace _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence
{
    public class ClearObstacleNode :BTActionNode
    {
        private Builder builder;

        public ClearObstacleNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            throw new System.NotImplementedException();
        }
    }
}