using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class IsArcherCooldownReadyNode : BTActionNode
    {
        private readonly Archer archer;

        public IsArcherCooldownReadyNode(Unit unit) : base(unit)
        {
            archer = unit as Archer;
        }

        public override BTStatus Tick()
        {
            if (archer.archerBlackBoard.detectedEnemy == null)
                return BTStatus.Failure;

            if (archer.animState == AnimState.Attacking) return BTStatus.Success;

            if (Time.time >= archer.nextFireTime) return BTStatus.Success;

            return BTStatus.Failure;
        }
    }
}