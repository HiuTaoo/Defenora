using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIdle
{
    public class HasNoEnemyInSight: BTActionNode
    {
        private Lancer lancer;
        public HasNoEnemyInSight(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            var dir = lancer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            if (lancer.DetectEnemies(lancer.viewDistance, dir).Count != 0) return BTStatus.Failure;
            lancer.ResetState();
            return BTStatus.Success;
        }
    }
}