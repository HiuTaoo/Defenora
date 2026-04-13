using _Script.BT.BlackBoard;
using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat
{
    public class WarriorAttackNode: BTActionNode
    {
        private Warrior warrior;

        public WarriorAttackNode(Unit unit) : base(unit)
        {
            this.warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (warrior.isKnockedBack)
            {
                ResetState();
                return BTStatus.Failure;
            }
            
            if (!warrior.IsEnemyInAttackRange())
            {
                ResetState();
                return BTStatus.Failure;
            }

            if (warrior.isAttacking)
            {
                return BTStatus.Running;
            }

            if (Time.time >= warrior.lastAttackTime + warrior.attackCooldown)
            {
                warrior.lastAttackTime = Time.time; 
                warrior.StartAttackSignal(); 
                
                warrior.currentState = UnitState.Defend; 
                warrior.animState = AnimState.Attacking;
            }
            else
            {
                warrior.currentState = UnitState.Defend;
                warrior.animState = AnimState.Idle;
            }

            return BTStatus.Running;
        }

        private void ResetState()
        {
            warrior.EndAttackSignal(); 
            warrior.currentState = UnitState.Idle;
            warrior.animState = AnimState.Idle;
        }
    }
}