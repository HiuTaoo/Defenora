using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class HasPlayerInTorchGoblinAttackRangeNode : BTActionNode
    {
        private readonly TorchGoblin goblin;

        public HasPlayerInTorchGoblinAttackRangeNode(TorchGoblin torchGoblin) : base(torchGoblin)
        {
            goblin = torchGoblin;
        }

        public override BTStatus Tick()
        {
            if (PlayerController.Instance == null)
            {
                if (goblin.currentTarget != null && goblin.currentTarget.CompareTag("Player"))
                    goblin.currentTarget = null;
                return BTStatus.Failure;
            }

            // Gọi hàm DetectPlayer truyền vào bán kính viewDistance (Tầm nhìn phát hiện) 
            // và hướng mặt GetCurrentFacingVector() của goblin
            var detectedPlayer = goblin.DetectPlayer(goblin.attackRange, goblin.GetCurrentFacingVector());

            if (detectedPlayer != null)
            {
                // Nhìn thấy -> Khóa mục tiêu là Player
                goblin.currentTarget = detectedPlayer.transform;
                goblin.currentTargetLayerIndex = PlayerController.Instance.GetCurrentLayerIndex();
                Debug.Log("Đã tìm thấy player");
                return BTStatus.Success;
            }

            // Mất dấu -> Nếu mục tiêu cũ là Player thì xóa đi để đổi sang mục tiêu khác
            if (goblin.currentTarget != null && goblin.currentTarget.CompareTag("Player")) goblin.currentTarget = null;
            return BTStatus.Failure;
        }
    }
}