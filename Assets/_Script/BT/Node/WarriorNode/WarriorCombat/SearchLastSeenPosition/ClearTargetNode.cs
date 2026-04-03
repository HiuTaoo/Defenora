using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition
{
    public class ClearTargetNode: BTActionNode
    {
        private Warrior warrior;

        public ClearTargetNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            warrior.lastSeenPosition = Vector2.zero;
            warrior.lastSeenLayerIndex = -999;
            warrior.currentTarget = null;
            Debug.Log("ClearTargetNode");
            return BTStatus.Success;
        }
    }
}