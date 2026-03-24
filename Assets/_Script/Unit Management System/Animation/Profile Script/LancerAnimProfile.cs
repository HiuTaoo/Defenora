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
            public UnitState state;
            public string animationName;
            public LancerDirection lancerDirection;
        }

        public List<StateAnimation> animations;
        public LancerDirection currentLancerDirection {get; private set;}

        public string GetAnimation(UnitState state)
        {
            var anim = animations.Find(a => a.state == state
            && a.lancerDirection == currentLancerDirection);
            return anim?.animationName;
        }

        public void SetLancerDirection(LancerDirection lancerDirection)
        {
            currentLancerDirection = lancerDirection;
        }
    }

}