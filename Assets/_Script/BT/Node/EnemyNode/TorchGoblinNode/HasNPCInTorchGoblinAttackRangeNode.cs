using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class HasNPCInTorchGoblinAttackRangeNode : BTActionNode
    {
        private TorchGoblin torchGoblin;

        public HasNPCInTorchGoblinAttackRangeNode(Unit unit) : base(unit)
        {
            torchGoblin = unit as TorchGoblin;
        }

        public override BTStatus Tick()
        {
            if (torchGoblin.currentTarget != null && 
                torchGoblin.currentTarget.CompareTag("NPC") && 
                torchGoblin.currentTarget.gameObject.activeInHierarchy)
            {
                float dist = Vector2.Distance(torchGoblin.transform.position, torchGoblin.currentTarget.position);
                if (dist <= torchGoblin.attackRange)
                {
                    return BTStatus.Success;
                }
            }

            var npcs = torchGoblin.DetectAllNPCsInRange(torchGoblin.attackRange);
            if (npcs.Count > 0)
            {
                var target = torchGoblin.SelectClosestTarget(npcs).transform;
                var unit = target.GetComponent<Unit>();
                
                if (unit == null || (unit is Archer && unit.assignedBuilding != null))
                    return BTStatus.Failure;

                torchGoblin.currentTarget = target;
                return BTStatus.Success;
            }
            
            return BTStatus.Failure;
        }
    }
}