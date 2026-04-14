using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.TNTGoblinNode
{
    public class EnsureBuildingTargetNode : BTActionNode
    {
        private TNTGoblin tntGoblin;

        public EnsureBuildingTargetNode(Unit unit) : base(unit)
        {
            tntGoblin = unit as TNTGoblin;
        }

        public override BTStatus Tick()
        {
            // 1. Nếu đang target một tòa nhà hợp lệ và chưa bị phá hủy -> Thành công, đi tiếp
            if (tntGoblin.currentTarget != null && tntGoblin.currentTarget.CompareTag("Building"))
            {
                var currentBuilding = tntGoblin.currentTarget.GetComponent<Building>();
                if (currentBuilding != null && currentBuilding.buildingState != BuildingState.Destroyed)
                {
                    return BTStatus.Success;
                }
            }

            // 2. Nếu mất mục tiêu (hoặc mục tiêu cũ là NPC vừa chết) -> Tìm tòa nhà gần nhất
            Building nearestBuilding = tntGoblin.FindNearestBuilding(tntGoblin.transform.position);
            
            if (nearestBuilding != null)
            {
                tntGoblin.currentTarget = nearestBuilding.transform;
                tntGoblin.currentTargetLayerIndex = nearestBuilding.layerIndex;
                return BTStatus.Success;
            }

            // Hết nhà trên bản đồ
            tntGoblin.currentTarget = null;
            return BTStatus.Failure;
        }
    }
}