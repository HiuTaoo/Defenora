using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat.WarriorArlert
{
    public class ClearAlertNode: BTActionNode
    {
        public ClearAlertNode(Unit unit):base(unit){}

        public override BTStatus Tick()
        {
            unit.isAlerted = false;
            unit.lastSeenLayerIndex = -1;
            unit.lastSeenPosition = Vector2.zero;
            Debug.Log($"{unit.transform.name}: Clear Alert");
            return BTStatus.Success;
        }
    }
}