using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat.ReturnToBuilding
{
    public class HasMaxDistanceExceeded: BTActionNode
    {
        private Warrior warrior;

        public HasMaxDistanceExceeded(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            var targetBuilding = warrior.assignedBuilding;

            if (targetBuilding == null)
            {
                float closestDistance = Mathf.Infinity;

                if (UnitManager.Instance != null && UnitManager.Instance.buildings != null)
                {
                    foreach (var b in UnitManager.Instance.buildings)
                    {
                        if (b == null) continue;

                        float dist = Vector2.Distance(warrior.transform.position, b.transform.position);
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            targetBuilding = b;
                        }
                    }
                }
            }

            if (targetBuilding == null) return BTStatus.Failure;

            var target = warrior.warriorBlackBoard.detectedEnemy;
            if (target == null) return BTStatus.Failure;

            float warriorDistToBuilding =  Vector2.Distance(warrior.transform.position, targetBuilding.transform.position);
            float targetDistToBuilding = Vector2.Distance(target.transform.position, targetBuilding.transform.position);

            if (warriorDistToBuilding >= targetBuilding.range && targetDistToBuilding > warriorDistToBuilding)
            {
                return BTStatus.Success; 
            }
            
            return BTStatus.Failure;
        }
    }
}