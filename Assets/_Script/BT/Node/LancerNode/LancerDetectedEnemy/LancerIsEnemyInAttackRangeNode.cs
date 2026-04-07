using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerDetectedEnemy.LancerCombatLoop
{
    public class LancerIsEnemyInAttackRangeNode: BTActionNode
    {
        private Lancer lancer;

        public LancerIsEnemyInAttackRangeNode(Unit unit) : base(unit)
        {
            this.lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if (lancer.IsEnemyInAttackRange())
            {
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}