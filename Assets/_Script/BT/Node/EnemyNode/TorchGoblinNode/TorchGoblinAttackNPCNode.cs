using _Script.Unit_Management_System.Enemy;
using UnityEngine; 

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class TorchGoblinAttackNPCNode: BTActionNode
    {
        private TorchGoblin torchGoblin;

        public TorchGoblinAttackNPCNode(Unit unit) : base(unit)
        {
            torchGoblin = unit as TorchGoblin;
        }

        public override BTStatus Tick()
        {
            if (torchGoblin.isKnockedBack)
            {
                ResetState();
                return BTStatus.Failure;
            }

            if (torchGoblin.currentTarget == null || !torchGoblin.currentTarget.gameObject.activeInHierarchy || !torchGoblin.currentTarget.CompareTag("NPC"))
            {
                torchGoblin.currentTarget = null;
                ResetState();
                return BTStatus.Failure;
            }

            var targetCol = torchGoblin.currentTarget.GetComponent<Collider2D>();
            float dist = Vector2.Distance(torchGoblin.transform.position, 
                targetCol != null ? targetCol.ClosestPoint(torchGoblin.transform.position) : torchGoblin.currentTarget.position);

            if (dist > torchGoblin.attackRange)
            {
                torchGoblin.currentTarget = null;
                ResetState();
                return BTStatus.Failure;
            }

            torchGoblin.characterMovement.RequestStopMoving();

            if (torchGoblin.isAttacking)
            {
                return BTStatus.Running;
            }

            if (Time.time >= torchGoblin.lastAttackTime + torchGoblin.attackCooldown)
            {
                torchGoblin.lastAttackTime = Time.time;
                torchGoblin.StartAttackSignal(); 

                torchGoblin.currentState = UnitState.Attack;
                torchGoblin.animState = AnimState.Attacking;
            }
            else
            {
                torchGoblin.currentState = UnitState.Idle;
                torchGoblin.animState = AnimState.Idle;
            }
            
            return BTStatus.Running;
        }
        
        private void ResetState()
        {
            torchGoblin.EndAttackSignal(); 
            torchGoblin.currentState = UnitState.Idle;
            torchGoblin.animState = AnimState.Idle;
        }
    }
}