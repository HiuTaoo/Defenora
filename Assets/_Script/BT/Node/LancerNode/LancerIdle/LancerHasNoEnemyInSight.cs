using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIdle
{
    public class LancerHasNoEnemyInSight: BTActionNode
    {
        private Lancer lancer;
        public LancerHasNoEnemyInSight(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            var dir = lancer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            if (lancer.DetectEnemies(lancer.viewDistance, dir).Count != 0 || lancer.currentTarget != null) return BTStatus.Failure;
            lancer.ResetState();
            return BTStatus.Success;
        }
    }
}