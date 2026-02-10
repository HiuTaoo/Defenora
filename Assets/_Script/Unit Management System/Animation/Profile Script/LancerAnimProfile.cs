using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile
{
    [CreateAssetMenu(menuName = "Animation/Lancer Animation Profile")]
    public class LancerAnimProfile : ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class StateAnimation
        {
            public UnitState state;
            public string animationName;
        }

        public List<StateAnimation> animations;

        public string GetAnimation(UnitState state)
        {
            var anim = animations.Find(a => a.state == state);
            return anim != null ? anim.animationName : null;
        }
    }

}