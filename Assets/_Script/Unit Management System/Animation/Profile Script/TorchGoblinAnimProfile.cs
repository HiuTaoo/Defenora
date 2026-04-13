using System.Collections.Generic;
using _Script.Enum;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile_Script
{
    [CreateAssetMenu(menuName = "CustomAnimation/TorchGoblin Animation Profile")]
    public class TorchGoblinAnimProfile: ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class StateAnimation
        {
            public UnitState unitState;
            public AnimState animState;
            public string animationName;
            public EnemyDirection enemyDirection;
        }

        public List<StateAnimation> animations;
        public EnemyDirection currentEnemyDirection {get; private set;}

        public string GetAnimation(UnitState unitState, AnimState animState)
        {
            var anim = animations.Find(
                a => a.unitState == unitState 
                     && a.animState == animState 
                     && a.enemyDirection == currentEnemyDirection);
            return anim?.animationName;
        }

        public void SetEnemyDirection(EnemyDirection enemyDirection)
        {
            currentEnemyDirection = enemyDirection;
        }
    }
}