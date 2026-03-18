namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class RotateScanNode: BTActionNode
    {
        private Archer archer;

        public RotateScanNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if(archer.archerBlackBoard.detectedEnemy != null)
                return BTStatus.Failure;
            
            archer.UpdateFacing(-archer.transform.localScale);
            return BTStatus.Success;
        }
    }
}