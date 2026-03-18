
using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class HasNoEnemyInRangeNode: BTActionNode
    {
        private Archer archer;

        public HasNoEnemyInRangeNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            var dir = archer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            if (archer.DetectEnemies(archer.attackRange, dir).Count == 0)
            {
                archer.ResetState();
                archer.animFSM.ChangeState(archer.currentState);
                return BTStatus.Success;
            }
            
            
            return BTStatus.Failure;
        }
    }
}