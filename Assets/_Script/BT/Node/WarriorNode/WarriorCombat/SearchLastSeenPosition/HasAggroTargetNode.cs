using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition
{
    public class HasAggroTargetNode: BTActionNode
    {
        private Warrior warrior;

        public HasAggroTargetNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (warrior.currentTarget != null)
            {
                return BTStatus.Success;
            }
                
            return BTStatus.Failure;
        }
    }
}