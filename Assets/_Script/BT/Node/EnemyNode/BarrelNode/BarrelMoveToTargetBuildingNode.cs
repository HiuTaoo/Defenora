using _Script.Unit_Management_System.Enemy;

namespace _Script.BT.Node.EnemyNode.BarrelNode
{
    public class BarrelMoveToTargetBuildingNode : BTActionNode
    {
        private Barrel barrel;
        private bool hasStartedMove = false;

        public BarrelMoveToTargetBuildingNode(Unit unit) : base(unit)
        {
            barrel = unit as Barrel;
        }

        public override BTStatus Tick()
        {
            if (barrel == null
                || barrel.currentState == UnitState.Dead 
                || barrel.currentTarget == null 
                || barrel.currentState == UnitState.Attack)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            if (barrel.CheckTargetBuildingInAttackRange())
            {
                StopBarrel();
                ResetNode();
                return BTStatus.Success;
            }

            if (barrel.CheckAndSetCloserBuildingTarget())
            {
                hasStartedMove = false; 
            }

            if (!hasStartedMove)
            {
                var path = barrel.FindBestPathToAnyAdjacentWithoutDiagonal(
                    barrel.currentTarget.gameObject,
                    barrel.currentTargetLayerIndex);

                if (path == null)
                {
                    ResetNode();
                    return BTStatus.Failure; 
                }

                barrel.characterMovement.RequestStopMoving();
                barrel.MoveToTargetPosition(path);

                barrel.currentState = UnitState.Move;
                barrel.animState = AnimState.Moving;

                hasStartedMove = true;
                return BTStatus.Running;
            }

            if (!barrel.characterMovement.moving)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            return BTStatus.Running;
        }

        private void StopBarrel()
        {
            barrel.characterMovement.RequestStopMoving();
            barrel.currentState = UnitState.Idle;
            barrel.animState = AnimState.Idle;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
        }
    }
}