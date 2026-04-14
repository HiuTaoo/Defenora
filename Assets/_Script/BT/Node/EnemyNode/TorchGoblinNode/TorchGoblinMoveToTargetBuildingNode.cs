using _Script.Unit_Management_System.Enemy;

namespace _Script.BT.Node.EnemyNode
{
    public class TorchGoblinMoveToTargetBuildingNode : BTActionNode
    {
        private TorchGoblin goblin;
        private bool hasStartedMove = false;

        public TorchGoblinMoveToTargetBuildingNode(Unit unit) : base(unit)
        {
            goblin = unit as TorchGoblin;
        }

        public override BTStatus Tick()
        {
            if (goblin.isKnockedBack || goblin.currentTarget == null)
            {
                ResetNode();
                return BTStatus.Failure; 
            }

            if (goblin.CheckNPCInAttackRange())
            {
                StopGoblin();
                ResetNode();
                return BTStatus.Failure; 
            }

            if (goblin.IsCollidingWithTarget(goblin.currentTarget.gameObject))
            {
                StopGoblin();
                ResetNode();
                return BTStatus.Success;
            }

            if (!hasStartedMove)
            {
                var path = goblin.FindBestPathToAnyAdjacentWithoutDiagonal(
                    goblin.currentTarget.gameObject, 
                    goblin.currentTargetLayerIndex);
                
                if (path != null && path.segments.Count > 0)
                {
                    goblin.characterMovement.RequestStopMoving();
                    goblin.MoveToTargetPosition(path);
                }
                
                hasStartedMove = true;
                
                goblin.currentState = UnitState.Move;
                goblin.animState = AnimState.Moving;

                return BTStatus.Running;
            }

            if (goblin.characterMovement.moving)
            {
                return BTStatus.Running;
            }

            goblin.MoveDirectlyToTarget(goblin.currentTarget.gameObject);
            
            goblin.currentState = UnitState.Move;
            goblin.animState = AnimState.Moving;

            return BTStatus.Running;
        }

        private void StopGoblin()
        {
            goblin.characterMovement.RequestStopMoving();
            goblin.currentState = UnitState.Idle;
            goblin.animState = AnimState.Idle;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
        }
    }
}