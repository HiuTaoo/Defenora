using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.TNTtntGoblinNode
{
    public class TNTGoblinMoveToTargetNode : BTActionNode
    {
        private TNTGoblin tntGoblin;
        private bool hasStartedMove = false;

        // Bộ đệm ghi nhớ tầng đất của lộ trình hiện tại để theo dõi đổi tầng giống TorchGoblin
        private int currentPathLayerIndex = -1;

        public TNTGoblinMoveToTargetNode(Unit unit) : base(unit)
        {
            tntGoblin = unit as TNTGoblin;
        }
        
        public override BTStatus Tick()
        {
            if (tntGoblin.isKnockedBack || tntGoblin.currentTarget == null ||
                tntGoblin.currentTarget.gameObject == null)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            var targetUnit = tntGoblin.currentTarget.GetComponent<Unit>();
            if (targetUnit != null)
                tntGoblin.currentTargetLayerIndex = targetUnit.characterMovement.CurrentLayer;
            else if (PlayerController.Instance != null)
                tntGoblin.currentTargetLayerIndex = PlayerController.Instance.floorAgent.currentFloorIndex;

            var npcs = tntGoblin.DetectNPCs(tntGoblin.attackRange, tntGoblin.GetCurrentFacingVector());
            if (npcs.Count > 0)
            {
                StopGoblin();
                ResetNode();
                return BTStatus.Success; 
            }
            
            if (tntGoblin.CheckAndSetNearbyBuildingTarget() || tntGoblin.CheckTargetBuildingInAttackRange())
            {
                StopGoblin();
                ResetNode();
                return BTStatus.Success;
            }

            if (currentPathLayerIndex != -1 && currentPathLayerIndex != tntGoblin.currentTargetLayerIndex)
            {
                hasStartedMove = false;
            }

            if (!hasStartedMove)
            {
                var targetGridPos = Vector3Int.FloorToInt(tntGoblin.currentTarget.position);
                targetGridPos.z = 0;

                PathFinding path = null;
                var isTargetNodeWalkable =
                    GraphNode.Instance.IsWalkableNode(targetGridPos, tntGoblin.currentTargetLayerIndex);

                if (!isTargetNodeWalkable)
                    path = tntGoblin.FindBestPathToAnyAdjacentWithoutDiagonal(
                        tntGoblin.currentTarget.gameObject,
                        tntGoblin.currentTargetLayerIndex);
                else
                    path = tntGoblin.FindBestPathToTarget(
                        tntGoblin.currentTarget.gameObject,
                        tntGoblin.currentTargetLayerIndex);

                if (path != null && path.segments != null && path.segments.Count > 0)
                {
                    tntGoblin.characterMovement.RequestStopMoving();
                    tntGoblin.MoveToTargetPosition(path);

                    hasStartedMove = true;
                    currentPathLayerIndex = tntGoblin.currentTargetLayerIndex;
                }
                else
                {
                    if (tntGoblin.currentTarget != null)
                        tntGoblin.MoveDirectlyToTarget(tntGoblin.currentTarget.gameObject);
                }

                return BTStatus.Running;
            }

            if (tntGoblin.characterMovement != null && tntGoblin.characterMovement.moving)
            {
                return BTStatus.Running;
            }

            hasStartedMove = false;

            if (tntGoblin.currentTarget != null) tntGoblin.MoveDirectlyToTarget(tntGoblin.currentTarget.gameObject);
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
            currentPathLayerIndex = -1;
        }
    }
}