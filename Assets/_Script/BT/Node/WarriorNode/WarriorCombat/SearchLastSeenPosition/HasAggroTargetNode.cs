using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition
{
    public class HasAggroTargetNode: BTActionNode
    {
        public HasAggroTargetNode(Unit unit) : base(unit) { }

        public override BTStatus Tick()
        {
            if (unit.currentTarget != null)
            {
                return BTStatus.Success;
            }
                
            return BTStatus.Failure;
        }
    }
}