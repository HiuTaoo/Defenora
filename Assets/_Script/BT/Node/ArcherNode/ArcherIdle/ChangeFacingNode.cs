namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class ChangeFacingNode: BTActionNode
    {
        private Archer archer;
        public ChangeFacingNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            archer.UpdateFacing(-archer.transform.localScale);
            return BTStatus.Success;
        }
    }
}