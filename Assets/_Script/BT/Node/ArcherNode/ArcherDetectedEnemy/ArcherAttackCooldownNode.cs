using _Script.Unit_Management_System.Animation;
using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class ArcherAttackCooldownNode : BTActionNode
    {
        private float timer = 0f;
        private Archer archer;

        public ArcherAttackCooldownNode(Unit unit) : base(unit)
        {
            archer = unit as Archer;
        }

        public override BTStatus Tick()
        {
            timer += Time.deltaTime;
            
            if(archer.archerBlackBoard.detectedEnemy == null)
                return BTStatus.Success;

            if (timer >= archer.fireRate)
            {
                
                timer = 0f;
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }
    }
}