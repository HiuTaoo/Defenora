using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode
{
    public class MoveToSpawnPointNode : BTActionNode
    {
        private bool hasStartedMove = false;
        private Vector3 targetWorldPos;

        public MoveToSpawnPointNode(Unit unit) : base(unit) {}

        public override BTStatus Tick()
        {
            var spawnPoint = unit.enemySpawnPoint;
            
            if (spawnPoint == null)
                return BTStatus.Failure;

            if (unit is Barrel && unit.currentState is UnitState.Dead or UnitState.Attack)
            {
                unit.characterMovement.RequestStopMoving(); 
                FinishMove();
                return BTStatus.Failure;
            }

            if (unit.currentState == UnitState.Dead)
            {
                unit.characterMovement.RequestStopMoving();
                FinishMove();
                return BTStatus.Failure;
            }

            targetWorldPos = spawnPoint.transform.position;

            if (unit.isKnockedBack)
            {
                hasStartedMove = false; 
                return BTStatus.Running;
            }

            if (!hasStartedMove)
            {
                var spawnPointLayerIndex = spawnPoint.GetComponent<SpawnPoint>().layerIndex;
                var path = unit.FindBestPathToTarget(spawnPoint, spawnPointLayerIndex);
                
                if (path == null)
                    return BTStatus.Failure;

                unit.characterMovement.RequestStopMoving(); 
                unit.MoveToTargetPosition(path);
                
                hasStartedMove = true;

                unit.currentState = UnitState.Move;
                unit.animState = AnimState.Moving;
            }

            if (hasStartedMove && unit.IsStopped())
            {
                hasStartedMove = false; 
            }

            float dist = Vector2.Distance(unit.transform.position, targetWorldPos);

            if (dist < 0.2f)
            {
                unit.characterMovement.RequestStopMoving();
                FinishMove();
                return BTStatus.Success; 
            }

            return BTStatus.Running;
        }

        private void FinishMove()
        {
            hasStartedMove = false;
            
            if(unit.currentState == UnitState.Move)
                unit.currentState = UnitState.Idle;
        }
    }
}