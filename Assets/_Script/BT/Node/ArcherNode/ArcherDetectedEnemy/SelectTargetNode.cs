using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class SelectTargetNode: BTActionNode
    {
        private Archer archer;

        public SelectTargetNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if(archer.archerBlackBoard.detectedEnemy != null)
                return BTStatus.Success;
            
            var dir = archer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            var list = archer.DetectEnemies(archer.attackRange, dir);
            var target = archer.SelectClosestTarget(list);
            if (target == null)
                return BTStatus.Failure;
            
            archer.archerBlackBoard.detectedEnemy = target;
            return BTStatus.Success;
        }
    }
}