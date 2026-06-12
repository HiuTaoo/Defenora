using _Script.Enum;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode
{
    public class EnemyFindNearestAttackerNode : BTActionNode
    {
        public EnemyFindNearestAttackerNode(Unit unit) : base(unit)
        {
        }

        public override BTStatus Tick()
        {
            // =================================================================
            // BƯỚC 1: QUÉT TÌM NPC TRONG TẦM NHÌN (ƯU TIÊN TỐI CAO)
            // =================================================================
            var allNPCs = unit.DetectAllNPCsInRange(unit.viewDistance);
            if (allNPCs != null && allNPCs.Count > 0)
            {
                var nearestNPC = unit.SelectClosestTarget(allNPCs);
                if (nearestNPC != null)
                {
                    unit.currentTarget = nearestNPC.transform;
                    var npcUnit = nearestNPC.GetComponent<Unit>();
                    unit.currentTargetLayerIndex = npcUnit != null
                        ? npcUnit.characterMovement.CurrentLayer
                        : unit.characterMovement.CurrentLayer;

                    return BTStatus.Success; // Bắt được lính -> Đi săn ngay lập tức!
                }
            }

            // =================================================================
            // BƯỚC 2: KHÔNG CÓ NPC -> QUÉT TÌM PLAYER (ƯU TIÊN BẬC HAI)
            // =================================================================
            if (PlayerController.Instance != null && PlayerController.Instance.gameObject.activeInHierarchy)
            {
                var distanceToPlayer =
                    Vector2.Distance(unit.transform.position, PlayerController.Instance.transform.position);

                // Chỉ nhắm vào Player nếu Player lọt vào tầm nhìnviewDistance của quái
                if (distanceToPlayer <= unit.viewDistance)
                {
                    unit.currentTarget = PlayerController.Instance.transform;
                    if (PlayerController.Instance.floorAgent != null)
                        unit.currentTargetLayerIndex = PlayerController.Instance.floorAgent.currentFloorIndex;

                    return BTStatus.Success;
                }
            }

            // =================================================================
            // BƯỚC 3: NẾU ĐANG CÓ MỤC TIÊU CŨ LÀ NHÀ (VÀ XUNG QUANH KHÔNG CÓ NPC/PLAYER VỪA XUẤT HIỆN)
            // =================================================================
            if (unit.currentTarget != null && unit.currentTarget.gameObject.activeInHierarchy)
                if (unit.currentTarget.CompareTag("Building"))
                {
                    var building = unit.currentTarget.GetComponent<Building>();
                    if (building != null && building.buildingState != BuildingState.Destroyed)
                        // Giữ nguyên mục tiêu đập nhà cũ vì xung quanh không có ai cản đường
                        return BTStatus.Success;
                }

            // =================================================================
            // BƯỚC 4: XUNG QUANH SẠCH BÓNG NGƯỜI -> ĐI TÌM NHÀ ĐỂ PHÁ (ƯU TIÊN CUỐI CÙNG)
            // =================================================================
            var nearestBuilding = unit.FindRandomBuilding(unit.transform.position);
            if (nearestBuilding != null)
            {
                unit.currentTarget = nearestBuilding.transform;
                unit.currentTargetLayerIndex = nearestBuilding.layerIndex;
                return BTStatus.Success;
            }

            // Không tìm được bất cứ thứ gì trên map để tương tác
            unit.currentTarget = null;
            return BTStatus.Failure;
        }
    }
}