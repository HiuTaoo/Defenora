using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class ShootArrowNode : BTActionNode
    {
        private Archer archer;

        private float timer = 0f;
        private float delay = 1f;
        private bool hasShot = false;

        public ShootArrowNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            var target = archer.archerBlackBoard.detectedEnemy;

            if (target == null)
            {
                ResetInternal();
                return BTStatus.Failure;
            }

            if (!hasShot)
            {
                Vector2 start = archer.firePoint.position;
                Vector2 end = target.transform.position;

                archer.animFSM.SetFireDirection(archer.archerBlackBoard.fireDirection);
                
                hasShot = true;
                timer = 0f;
            }

            timer += Time.deltaTime;

            if (timer >= delay)
            {
                archer.ResetAnim();
                ResetInternal();
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        private void ResetInternal()
        {
            hasShot = false;
            timer = 0f;
        }
    }
}