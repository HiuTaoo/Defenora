namespace _Script.BT.Node.EnemyNode
{
    public class ResetStateNode: BTActionNode
    {
        public ResetStateNode(Unit unit): base(unit){}

        public override BTStatus Tick()
        {
            unit.EnemyResetState();
            return BTStatus.Success;
        }
    }
}