using _Script.BT.Node;
using _Script.Unit_Management_System.Enemy;
using UnityEngine;

public class HasPlayerInTNTGoblinAttackRangeNode : BTActionNode
{
    private readonly TNTGoblin goblin;

    public HasPlayerInTNTGoblinAttackRangeNode(TNTGoblin tntGoblin) : base(tntGoblin)
    {
        goblin = tntGoblin;
    }

    public override BTStatus Tick()
    {
        if (PlayerController.Instance == null)
        {
            if (goblin.currentTarget != null && goblin.currentTarget.CompareTag("Player"))
                goblin.currentTarget = null;
            return BTStatus.Failure;
        }

        var detectedPlayer = goblin.DetectPlayer(goblin.attackRange, goblin.GetCurrentFacingVector());

        if (detectedPlayer != null)
        {
            goblin.currentTarget = detectedPlayer.transform;
            goblin.currentTargetLayerIndex =
                PlayerController.Instance.floorAgent
                    .currentFloorIndex; // Sửa lỗi gọi GetCurrentLayerIndex sai kiểu dữ liệu của PlayerController

            Debug.Log($"[{goblin.gameObject.name}] 🟢 Đã tìm thấy player trong vùng đánh xa bằng hàm DetectPlayer!");
            return BTStatus.Success;
        }

        return BTStatus.Failure;
    }
}