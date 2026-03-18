using _Script.Unit_Management_System.Animation;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class AimAtTargetNode: BTActionNode
    {
        private Archer archer;

        public AimAtTargetNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            var target = archer.archerBlackBoard.detectedEnemy;

            if (target == null)
                return BTStatus.Failure;
            
            ArcherFireDirection dir = archer.GetFireDirection(archer.transform.position, target.transform.position);

            archer.archerBlackBoard.fireDirection = dir;

            return BTStatus.Success;
        }
    }
}