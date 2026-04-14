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

            var npcs = torchGoblin.DetectNPCs(torchGoblin.attackRange, torchGoblin.GetCurrentFacingVector());
            if (npcs.Count == 0)
            {
                ResetState();
                return BTStatus.Failure;
            }

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