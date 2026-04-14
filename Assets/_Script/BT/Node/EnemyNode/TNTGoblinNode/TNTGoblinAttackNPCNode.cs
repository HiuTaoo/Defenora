using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.TNTGoblinNode
{
    public class TNTGoblinAttackNPCNode: BTActionNode
    {
        private TNTGoblin tntGoblin;

        public TNTGoblinAttackNPCNode(Unit unit) : base(unit)
        {
            tntGoblin = unit as TNTGoblin;
        }
        
        public override BTStatus Tick()
        {
            if (tntGoblin.isKnockedBack)
            {
                ResetState();
                return BTStatus.Failure;
            }

            if (tntGoblin.subTarget == null || !tntGoblin.subTarget.activeInHierarchy
                || tntGoblin.subTarget.GetComponent<Unit>().currentState == UnitState.Dead)
            {
                tntGoblin.subTarget = null;
                ResetState();
                return BTStatus.Failure;
            }

            var targetCol = tntGoblin.subTarget.GetComponent<Collider2D>();
            float dist;

            if (targetCol != null && !targetCol.isTrigger)
            {
                Vector2 closestPoint = targetCol.ClosestPoint(tntGoblin.transform.position);
                dist = Vector2.Distance(tntGoblin.transform.position, closestPoint);
            }
            else
            {
                dist = Vector2.Distance(tntGoblin.transform.position, tntGoblin.subTarget.transform.position);
            }

            if (dist > tntGoblin.attackRange)
            {
                tntGoblin.subTarget = null;
                ResetState();
                return BTStatus.Failure;
            }

            tntGoblin.characterMovement.RequestStopMoving();

            if (tntGoblin.isAttacking)
            {
                return BTStatus.Running;
            }

            if (Time.time >= tntGoblin.lastAttackTime + tntGoblin.attackCooldown)
            {
                tntGoblin.lastAttackTime = Time.time;
                tntGoblin.StartAttackSignal(); 

                tntGoblin.currentState = UnitState.Attack;
                tntGoblin.animState = AnimState.Attacking;
            }
            else
            {
                tntGoblin.currentState = UnitState.Idle;
                tntGoblin.animState = AnimState.Idle;
            }
            
            return BTStatus.Running;
        }
        
        private void ResetState()
        {
            tntGoblin.EndAttackSignal(); 
            tntGoblin.currentState = UnitState.Idle;
            tntGoblin.animState = AnimState.Idle;
        }
    }
}