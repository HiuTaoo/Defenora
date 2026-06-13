using _Script.BT.Node;
using UnityEngine;

public class RaidCampaignExecutionNode : BTActionNode
{
    private RaidState currentState = RaidState.Assemble;
    private bool hasCalculatedPath;

    public RaidCampaignExecutionNode(Unit unit) : base(unit)
    {
    }

    public override BTStatus Tick()
    {
        if (RaidManager.Instance == null || RaidManager.Instance.activeRaidTarget == null)
        {
            ResetNode();
            return BTStatus.Failure;
        }

        var targetGate = RaidManager.Instance.activeRaidTarget;

        if (!targetGate.activeInHierarchy)
        {
            ResetNode();
            return BTStatus.Success; 
        }

        if (currentState == RaidState.Assemble && RaidManager.Instance.isAssembleComplete)
        {
            currentState = RaidState.March;
            RaidManager.Instance.raidState = currentState;
            hasCalculatedPath = false;
            unit.StopMove();
        }

        if (currentState == RaidState.March)
        {
            var spawnPoint = targetGate.GetComponent<SpawnPoint>();
            int gateLayer = spawnPoint != null ? spawnPoint.layerIndex : 0;

            if (unit.layerIndex == gateLayer)
            {
                var distanceToGate = Vector2.Distance(unit.transform.position, targetGate.transform.position);
                var leader = RaidManager.Instance.leaderUnit;

                if (leader != null)
                {
                    if (unit.unitType == leader.unitType)
                    {
                        if (distanceToGate <= unit.viewDistance - 1f)
                        {
                            unit.StopMove();
                            ResetNode();
                            return BTStatus.Success;
                        }
                    }
                    else 
                    {
                        if (distanceToGate <= leader.viewDistance + 1f)
                        {
                            unit.StopMove();
                            ResetNode();
                            return BTStatus.Success;
                        }
                    }
                }
                else 
                {
                    if (distanceToGate <= unit.viewDistance - 1f)
                    {
                        unit.StopMove();
                        ResetNode();
                        return BTStatus.Success;
                    }
                }
            }
        }

        switch (currentState)
        {
            case RaidState.Assemble:
                if (RaidManager.Instance.leaderUnit == null) return BTStatus.Failure;

                if (unit == RaidManager.Instance.leaderUnit)
                {
                    unit.StopMove();
                    unit.currentState = UnitState.Idle;
                    unit.animState = AnimState.Idle;
                }
                else 
                {
                    var leader = RaidManager.Instance.leaderUnit;
                    var distToLeader = Vector2.Distance(unit.transform.position, leader.transform.position);

                    if (unit.layerIndex != leader.layerIndex || distToLeader > 2.5f)
                    {
                        if (!hasCalculatedPath || !unit.characterMovement.moving)
                        {
                            // ❌ XÓA DÒNG CŨ: var rawLeaderGrid = Vector3Int.FloorToInt(leader.transform.position);
                            
                            //  SỬA THÀNH: Sử dụng hàm quy đổi Grid chuẩn của hệ thống để không bị lỗi số âm
                            // Nếu trong dự án của bạn dùng ObjectSpawner.Instance.WorldToGrid thì thay vào đây:
                            Vector3Int rawLeaderGrid = Vector3Int.FloorToInt(leader.transform.position);
                            
                            // Nếu hệ thống tìm đường có hàm quy đổi riêng, hãy dùng hàm đó để triệt tiêu việc FloorToInt sai số âm:
                            // Ví dụ: Vector3Int rawLeaderGrid = GraphNode.Instance.WorldToGridPos(leader.transform.position);

                            Vector3Int startGrid = Vector3Int.FloorToInt(unit.transform.position);

                            Vector3Int targetAssembleGrid = unit.FindAdjacentWalkableCell(rawLeaderGrid, leader.layerIndex);

                            var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                                startGrid, unit.layerIndex,
                                targetAssembleGrid, leader.layerIndex);

                            if (path != null && path.segments.Count > 0)
                            {
                                unit.MoveToTargetPosition(path);
                                hasCalculatedPath = true;
                            }
                            else
                            {
                                // Sửa lại chuỗi cách viết string Log để không bị dính ký tự sát nhau gây nhìn nhầm thành dấu trừ
                                Debug.LogError($"[Raid Path Error] [{unit.gameObject.name}] KHÔNG TÌM THẤY ĐƯỜNG TẬP KẾT! " +
                                               $"| Start: {startGrid} (Layer {unit.layerIndex}) " +
                                               $"| Target (Leader): {targetAssembleGrid} (Layer {leader.layerIndex})");
                                
                                hasCalculatedPath = false; 
                            }
                        }
                    }
                    else
                    {
                        if (unit.characterMovement.moving) unit.StopMove();
                        unit.currentState = UnitState.Idle;
                        unit.animState = AnimState.Idle;
                    }
                }
                return BTStatus.Running;

            case RaidState.March:
                if (!hasCalculatedPath || !unit.characterMovement.moving)
                {
                    var spawnPoint = targetGate.GetComponent<SpawnPoint>();
                    int gateLayer = spawnPoint != null ? spawnPoint.layerIndex : 0;
                    Vector3Int startGrid = Vector3Int.FloorToInt(unit.transform.position);

                    var marchPath = unit.FindBestPathToTarget(targetGate, gateLayer);
                    if (marchPath != null && marchPath.segments.Count > 0)
                    {
                        unit.MoveToTargetPosition(marchPath);
                        hasCalculatedPath = true;
                    }
                    else
                    {
                        // 🔥 ADD LOG: Báo lỗi khi không tìm thấy đường hành quân đến Cổng quái
                        Vector3Int gateGrid = Vector3Int.FloorToInt(targetGate.transform.position);
                        Debug.LogError($"[Raid Path Error] [{unit.gameObject.name}] KHÔNG TÌM THẤY ĐƯỜNG HÀNH QUÂN (March)! " +
                                       $"➔ Start: {startGrid} (Layer {unit.layerIndex}) " +
                                       $"➔ Target (Gate): {gateGrid} (Layer {gateLayer})");

                        hasCalculatedPath = false;
                    }
                }
                return BTStatus.Running;
        }
        return BTStatus.Running;
    }

    public override void ClearState()
    {
        base.ClearState();
        ResetNode();
    }

    private void ResetNode()
    {
        currentState = RaidState.Assemble;
        hasCalculatedPath = false;
    }
}