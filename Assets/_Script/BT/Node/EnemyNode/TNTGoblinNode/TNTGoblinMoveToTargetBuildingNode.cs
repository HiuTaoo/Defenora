using _Script.Unit_Management_System.Enemy;

namespace _Script.BT.Node.EnemyNode.TNTtntGoblinNode
{
    public class TNTGoblinMoveToTargetBuildingNode: BTActionNode
    {
        private TNTGoblin tntGoblin;
        private bool hasStartedMove = false;

        public TNTGoblinMoveToTargetBuildingNode(Unit unit) : base(unit)
        {
            tntGoblin = unit as TNTGoblin;
        }
        
        public override BTStatus Tick()
        {
            if (tntGoblin.isKnockedBack || tntGoblin.currentTarget == null)
            {
                ResetNode();
                return BTStatus.Failure; 
            }

            var npcs = tntGoblin.DetectNPCs(tntGoblin.attackRange,
                tntGoblin.GetCurrentFacingVector());
            
            if (npcs.Count > 0)
            {
                StopGoblin();
                ResetNode();
                return BTStatus.Failure; 
            }
            
            if (tntGoblin.CheckAndSetNearbyBuildingTarget() || tntGoblin.CheckTargetBuildingInAttackRange())
            {
                StopGoblin();
                ResetNode();
                return BTStatus.Success;
            }

            if (!hasStartedMove)
            {
                var path = tntGoblin.FindBestPathToAnyAdjacentWithoutDiagonal(
                    tntGoblin.currentTarget.gameObject, 
                    tntGoblin.currentTargetLayerIndex);
                
                if (path == null)
                {
                    ResetNode();
                    return BTStatus.Failure;
                }

                tntGoblin.characterMovement.RequestStopMoving();
                tntGoblin.MoveToTargetPosition(path);

                tntGoblin.currentState = UnitState.Move;
                tntGoblin.animState = AnimState.Moving;

                hasStartedMove = true;
                return BTStatus.Running;
            }

            if (!tntGoblin.characterMovement.moving)
            {
                ResetNode();
                return BTStatus.Failure; 
            }

            return BTStatus.Running;
        }

        private void StopGoblin()
        {
            tntGoblin.characterMovement.RequestStopMoving();
            tntGoblin.currentState = UnitState.Idle;
            tntGoblin.animState = AnimState.Idle;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
        }
    }
}