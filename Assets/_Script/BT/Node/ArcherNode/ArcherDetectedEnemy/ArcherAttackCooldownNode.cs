using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class ArcherAttackCooldownNode : BTActionNode
    {
        private readonly Archer archer;
        private float timer;

        public ArcherAttackCooldownNode(Unit unit) : base(unit)
        {
            archer = unit as Archer;
        }

        public override BTStatus Tick()
        {
            timer += Time.deltaTime;

            if (archer.archerBlackBoard.detectedEnemy == null)
            {
                timer = 0f;
                return BTStatus.Success;
            }

            if (timer >= archer.attackCooldown)
            {
                timer = 0f;
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        public override void ClearState()
        {
            base.ClearState();
            timer = 0f;
        }
    }
}