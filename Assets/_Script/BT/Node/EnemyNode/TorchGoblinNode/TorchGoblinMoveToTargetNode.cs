using _Script.BT.Node.EnemyNode.unitNode;
using _Script.Enum;
using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode
{
    public class TorchGoblinMoveToTargetNode : BTActionNode
    {
        private TorchGoblin goblin;
        private bool hasStartedMove = false;
        
        private int currentPathLayerIndex = -1;

        public TorchGoblinMoveToTargetNode(Unit unit) : base(unit)
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
            
            var targetUnit = goblin.currentTarget.GetComponent<Unit>();
            if (targetUnit != null)
            {
                goblin.currentTargetLayerIndex = targetUnit.characterMovement.CurrentLayer;
            }
            else if (PlayerController.Instance != null)
            {
                goblin.currentTargetLayerIndex = PlayerController.Instance.floorAgent.currentFloorIndex;
            }

            if (goblin.CheckNPCInAttackRange() || goblin.CheckPlayerInAttackRange())
            {
                StopGoblin();
                ResetNode();
                return BTStatus.Success; 
            }

            if (goblin.IsCollidingWithTarget(goblin.currentTarget.gameObject))
            {
                StopGoblin();
                ResetNode();
                return BTStatus.Success;
            }

            if (hasStartedMove && currentPathLayerIndex != goblin.currentTargetLayerIndex)
            {
                Debug.Log($"[{goblin.gameObject.name}] Phát hiện mục tiêu đổi tầng (Từ Layer {currentPathLayerIndex + 1} sang {goblin.currentTargetLayerIndex + 1})! Tính toán lại đường đi đa tầng...");
                hasStartedMove = false;
            }

            if (!hasStartedMove)
            {
                var targetGridPos = Vector3Int.FloorToInt(goblin.currentTarget.position);
                targetGridPos.z = 0;

                PathFinding path = null;
                var isTargetNodeWalkable = GraphNode.Instance.IsWalkableNode(targetGridPos, goblin.currentTargetLayerIndex);

                if (!isTargetNodeWalkable)
                {
                    path = goblin.FindBestPathToAnyAdjacentWithoutDiagonal(
                        goblin.currentTarget.gameObject,
                        goblin.currentTargetLayerIndex);
                }
                else
                {
                    path = goblin.FindBestPathToTarget(
                        goblin.currentTarget.gameObject,
                        goblin.currentTargetLayerIndex);
                }

                if (path != null && path.segments != null && path.segments.Count > 0)
                {
                    goblin.characterMovement.RequestStopMoving();
                    goblin.MoveToTargetPosition(path);
                    
                    hasStartedMove = true;
                    currentPathLayerIndex = goblin.currentTargetLayerIndex; 
                }
                else
                {
                    goblin.MoveDirectlyToTarget(goblin.currentTarget.gameObject);
                }

                return BTStatus.Running;
            }

            if (goblin.characterMovement != null && goblin.characterMovement.moving)
            {
                return BTStatus.Running;
            }

            hasStartedMove = false;
            goblin.MoveDirectlyToTarget(goblin.currentTarget.gameObject);
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
            currentPathLayerIndex = -1;
        }
    }
}