using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIntercept
{
    public class LancerHasEnemyInSightNode: BTActionNode
    {
        private Lancer lancer;

        public LancerHasEnemyInSightNode(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if (lancer.lancerBlackBoard.detectedEnemy != null)
                return BTStatus.Success;
            
            var dir = lancer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            var enemies = lancer.DetectEnemies(lancer.viewDistance, dir);
            if (enemies.Count == 0 || lancer.currentTarget == null)
            {
                var target = lancer.SelectClosestTarget(enemies);

                if (target == null)
                {
                    return BTStatus.Failure;
                }

                lancer.lancerBlackBoard.detectedEnemy = target;
                lancer.currentState = UnitState.Defend;
                return BTStatus.Success;
            }
               
            return BTStatus.Failure;
        }
    }
}