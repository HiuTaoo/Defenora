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
            if (!warrior.IsEnemyInAttackRange())
                return BTStatus.Failure;
            
            warrior.currentState = UnitState.Attacking;
            Debug.Log(warrior.animFSM.GetWarriorDirection());
            return BTStatus.Running;
        }
    }
}