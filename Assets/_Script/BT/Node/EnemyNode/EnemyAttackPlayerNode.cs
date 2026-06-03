using UnityEngine;

namespace _Script.BT.Node.EnemyNode
{
    public class EnemyAttackPlayerNode : BTActionNode
    {
        private bool hasStartedMove;
        private Vector3Int lastTargetGridPos = new(-999, -999, -999);

        public EnemyAttackPlayerNode(Unit unit) : base(unit)
        {
        }

        public override BTStatus Tick()
        {
            if (unit.isKnockedBack || unit.currentTarget == null || !unit.currentTarget.gameObject.activeInHierarchy ||
                !unit.currentTarget.CompareTag("Player"))
            {
                ResetNode();
                return BTStatus.Failure;
            }

            var dist = Vector2.Distance(unit.transform.position, unit.currentTarget.position);

            if (dist <= unit.attackRange)
            {
                if (unit.characterMovement != null && unit.characterMovement.moving)
                {
                    unit.characterMovement.RequestStopMoving();
                    var rb2d = unit.GetComponent<Rigidbody2D>();
                    if (rb2d != null) rb2d.velocity = Vector2.zero;
                }

                if (Time.time >= unit.lastAttackTime + unit.attackCooldown)
                {
                    if (unit.isAttacking) unit.EndAttackSignal();

                    unit.lastAttackTime = Time.time;
                    unit.StartAttackSignal();

                    unit.currentState = UnitState.Attack;
                    unit.animState = AnimState.Attacking;

                    if (unit.animFSM != null) unit.animFSM.ChangeState(unit.currentState, unit.animState);
                }
                else
                {
                    if (unit.isAttacking) return BTStatus.Running;

                    unit.currentState = UnitState.Idle;
                    unit.animState = AnimState.Idle;

                    if (unit.animFSM != null) unit.animFSM.ChangeState(unit.currentState, unit.animState);
                }

                hasStartedMove = false;
                return BTStatus.Running;
            }

            var playerGridPos = Vector3Int.FloorToInt(unit.currentTarget.position);
            playerGridPos.z = 0;

            var isPlayerFledFar = Vector3Int.Distance(playerGridPos, lastTargetGridPos) > 2;

            if (!hasStartedMove || isPlayerFledFar)
            {
                lastTargetGridPos = playerGridPos;

                var path = unit.FindBestPathToTarget(unit.currentTarget.gameObject, unit.currentTargetLayerIndex);

                if (path != null && path.segments != null && path.segments.Count > 0)
                {
                    unit.currentState = UnitState.Move;
                    unit.animState = AnimState.Moving;
                    unit.MoveToTargetPosition(path);
                    hasStartedMove = true;
                }

                return BTStatus.Running;
            }

            if (unit.characterMovement != null && unit.characterMovement.moving)
            {
                unit.currentState = UnitState.Move;
                unit.animState = AnimState.Moving;
                return BTStatus.Running;
            }

            hasStartedMove = false;
            return BTStatus.Running;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
            lastTargetGridPos = new Vector3Int(-999, -999, -999);

            unit.EndAttackSignal();
            if (unit.characterMovement != null) unit.characterMovement.RequestStopMoving();
            unit.currentState = UnitState.Idle;
            unit.animState = AnimState.Idle;
        }
    }
}