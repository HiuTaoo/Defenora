using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorIdle
{
    public class WarriorHasNoEnemyInSightNode: BTActionNode
    {
        private Warrior warrior;

        public WarriorHasNoEnemyInSightNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            var dir = warrior.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            if (warrior.DetectEnemies(warrior.viewDistance, dir).Count != 0) return BTStatus.Failure;
            warrior.ResetState();
            return BTStatus.Success;
        }
    }
}