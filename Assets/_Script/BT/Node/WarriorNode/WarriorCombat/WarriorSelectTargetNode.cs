using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat
{
    public class WarriorSelectTargetNode: BTActionNode
    {
        private Warrior warrior;

        public WarriorSelectTargetNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            var dir = warrior.transform.localScale.x > 0 ? Vector2.right : Vector2.left;

            var enemies = warrior.DetectEnemies(warrior.viewDistance, dir);
            
            var target = warrior.SelectClosestTarget(enemies);

            if (target == null)
            {
                return BTStatus.Failure;
            }

            warrior.warriorBlackBoard.detectedEnemy = target;
            warrior.currentState = UnitState.Defend;

            return BTStatus.Success;
        }
    }
}