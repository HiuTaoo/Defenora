using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class HasDetectedEnemyNode: BTActionNode
    {
        private Archer archer;
        public HasDetectedEnemyNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            var dir = archer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            if(archer.DetectEnemies(archer.attackRange, dir).Count == 0)
                return BTStatus.Failure;

            archer.currentState = UnitState.Defend;
            archer.animState = AnimState.Attacking;
            return BTStatus.Success;
        }
    }
}