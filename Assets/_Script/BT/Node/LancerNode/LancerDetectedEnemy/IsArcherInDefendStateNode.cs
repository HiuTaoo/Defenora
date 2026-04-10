namespace _Script.BT.Node.LancerNode.LancerDetectedEnemy
{
    public class IsArcherInDefendStateNode: BTActionNode
    {
        private Archer archer;

        public IsArcherInDefendStateNode(Unit unit) : base(unit)
        {
            archer = unit as Archer;
        }

        public override BTStatus Tick()
        {
            if (archer.archerBlackBoard != null && archer.archerBlackBoard.detectedEnemy != null &&
                archer.currentTarget != null)
            {
                archer.currentState = UnitState.Defend;
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}