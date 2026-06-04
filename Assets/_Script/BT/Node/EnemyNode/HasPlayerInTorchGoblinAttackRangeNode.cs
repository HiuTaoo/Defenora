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

            var detectedPlayer = goblin.DetectPlayer(goblin.attackRange, goblin.GetCurrentFacingVector());

            if (detectedPlayer != null)
            {
                goblin.currentTarget = detectedPlayer.transform;
                goblin.currentTargetLayerIndex = PlayerController.Instance.GetCurrentLayerIndex();
                Debug.Log("Đã tìm thấy player");
                return BTStatus.Success;
            }

            return BTStatus.Failure;
        }
    }
}