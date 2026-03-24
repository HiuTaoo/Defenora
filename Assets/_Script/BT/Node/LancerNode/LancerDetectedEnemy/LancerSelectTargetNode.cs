using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerDetectedEnemy
{
    public class LancerSelectTargetNode: BTActionNode
    {
        private Lancer lancer;
        public LancerSelectTargetNode(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            var dir = lancer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;

            var enemies = lancer.DetectEnemies(lancer.viewDistance, dir);

            /*if (enemies.Count == 0)
            {
                lancer.lancerBlackBoard.detectedEnemy = null;
                return BTStatus.Failure;
            }*/

            var target = lancer.SelectClosestTarget(enemies);

            if (target == null)
            {
                //lancer.lancerBlackBoard.detectedEnemy = null;
                return BTStatus.Failure;
            }

            lancer.lancerBlackBoard.detectedEnemy = target;

            return BTStatus.Success;
        }
    }
}