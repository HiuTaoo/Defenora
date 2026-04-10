using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat.WarriorArlert
{
    public class IsUnitAlertedNode: BTActionNode
    {
     public IsUnitAlertedNode(Unit unit):base(unit){}

     public override BTStatus Tick()
     {
         return unit.isAlerted ? BTStatus.Success : BTStatus.Failure;
     }
    }
}