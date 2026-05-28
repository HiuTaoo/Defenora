using _Script.Enum;
using _Script.Unit_Management_System.Enemy;
using UnityEngine; 

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class TorchGoblinAttack: BTActionNode
    {
        private TorchGoblin torchGoblin;

        public TorchGoblinAttack(Unit unit) : base(unit)
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

            if (torchGoblin.currentTarget == null) 
            {
                ResetState();
                return BTStatus.Failure;
            }

            var building = torchGoblin.currentTarget.GetComponent<Building>();
            if (building == null || building.buildingState == BuildingState.Destroyed)
            {
                ResetState();
                return BTStatus.Success; 
            }

            if (!torchGoblin.CheckTargetBuildingInAttackRange())
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