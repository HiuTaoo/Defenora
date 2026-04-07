using UnityEngine;

namespace _Script.BT.Node.LancerNode
{
    public class IsDefendStateNode: BTActionNode
    {
        private Lancer lancer;

        public IsDefendStateNode(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if (lancer.lancerBlackBoard.detectedEnemy != null || lancer.currentTarget != null)
            {
                lancer.currentState = UnitState.Defend;
                return BTStatus.Success;
            } 
            Debug.Log("enemy null");
            return BTStatus.Failure;
        }
    }
}