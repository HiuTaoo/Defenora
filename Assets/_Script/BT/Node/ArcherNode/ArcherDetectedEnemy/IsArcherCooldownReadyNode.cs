using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class IsArcherCooldownReadyNode : BTActionNode
    {
        private Archer archer;

        public IsArcherCooldownReadyNode(Unit unit) : base(unit)
        {
            archer = unit as Archer;
        }

        public override BTStatus Tick()
        {
            if (archer.archerBlackBoard.detectedEnemy == null)
                return BTStatus.Failure;

            if (Time.time >= archer.nextFireTime)
            {
                return BTStatus.Success; 
            }

            return BTStatus.Failure; 
        }
    }
}