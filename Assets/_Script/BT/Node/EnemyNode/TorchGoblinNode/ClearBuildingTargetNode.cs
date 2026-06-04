using _Script.Enum;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class ClearBuildingTargetNode : BTActionNode
    {
        public ClearBuildingTargetNode(Unit unit) : base(unit) { }

        public override BTStatus Tick()
        {
            if (unit.currentTarget == null)
                return BTStatus.Failure;

            bool shouldClear = false;

            if (!unit.currentTarget.gameObject.activeInHierarchy)
            {
                shouldClear = true;
            }
            else if (unit.currentTarget.CompareTag("Building"))
            {
                var building = unit.currentTarget.GetComponent<Building>();
                if (building == null || building.buildingState == BuildingState.Destroyed)
                {
                    shouldClear = true;
                }
            }
            else if (unit.currentTarget.CompareTag("NPC"))
            {
                var health = unit.currentTarget.GetComponentInChildren<Health>();
                if (health == null || health.CurrentHealth <= 0)
                {
                    shouldClear = true;
                }
            }

            if (shouldClear)
            {
                Debug.Log($"[{unit.gameObject.name}] 🎯 Mục tiêu [{unit.currentTarget.name}] đã bị tiêu diệt hoàn toàn. Tiến hành xóa dữ liệu để tìm con mồi mới!");
                
                unit.currentTarget = null;
                unit.ResetAnim();
                return BTStatus.Success;
            }

            return BTStatus.Failure;
        }
    }
}