using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerDetectedEnemy.LancerCombatLoop
{
    public class LancerAttackNode: BTActionNode
    {
        private Lancer lancer;

        public LancerAttackNode(Unit unit) : base(unit)
        {
            this.lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if (lancer.isKnockedBack)
            {
                lancer.EndAttackSignal();
                ResetState();
                return BTStatus.Failure;
            }
            
            if( lancer.currentTarget == null){
                lancer.currentState = UnitState.Idle;
                lancer.animState = AnimState.Idle;
                lancer.ClearAggro();
                return BTStatus.Failure;
            }

            if (!lancer.IsEnemyInAttackRange())
            {
                ResetState();
                return BTStatus.Failure;
            }

            if (lancer.isAttacking)
            {
                return BTStatus.Running;
            }

            if (Time.time >= lancer.lastAttackTime + lancer.attackCooldown)
            {
                lancer.lastAttackTime = Time.time;
                lancer.StartAttackSignal(); 
                
                lancer.currentState = UnitState.Defend; 
                lancer.animState = AnimState.Attacking;
            }
            else
            {
                lancer.currentState = UnitState.Defend;
                lancer.animState = AnimState.Defending;
            }

            return BTStatus.Running;
        }

        private void ResetState()
        {
            lancer.EndAttackSignal();
            lancer.currentState = UnitState.Defend;
            lancer.animState = AnimState.Defending;
        }
    }
}