using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile_Script
{
    [CreateAssetMenu(menuName = "Animation/Lancer Animation Profile")]
    public class LancerAnimProfile : ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class StateAnimation
        {
            public UnitState UnitState;
            public AnimState animState;
            public string animationName;
            public LancerDirection lancerDirection;
        }

        public List<StateAnimation> animations;
        public LancerDirection currentLancerDirection {get; private set;}

        public string GetAnimation(UnitState unitState, AnimState animState)
        {
            var anim = animations.Find(
                a => a.UnitState == unitState 
                     && a.animState == animState 
                     && a.lancerDirection == currentLancerDirection);
            return anim?.animationName;
        }

        public void SetLancerDirection(LancerDirection lancerDirection)
        {
            currentLancerDirection = lancerDirection;
        }
    }

}