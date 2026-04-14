using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.TNTGoblinNode
{
    public class TNTGoblinAttack: BTActionNode
    {
        private TNTGoblin tntGoblin;

        public TNTGoblinAttack(Unit unit) : base(unit)
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

            if (tntGoblin.currentTarget == null) 
            {
                ResetState();
                return BTStatus.Failure;
            }

            var building = tntGoblin.currentTarget.GetComponent<Building>();
            if (building == null || building.buildingState == BuildingState.Destroyed)
            {
                ResetState();
                return BTStatus.Success; 
            }

            if (!tntGoblin.CheckTargetBuildingInAttackRange())
            {
                ResetState();
                return BTStatus.Failure; 
            }

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