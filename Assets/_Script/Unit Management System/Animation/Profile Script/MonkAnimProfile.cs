using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile_Script
{
    [CreateAssetMenu(menuName = "Animation/Monk Animation Profile")]
    public class MonkAnimProfile: ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class MonkStateAnimation
        {
            public UnitState unitState;
            public AnimState animState;
            public string animationName;
        }

        public List<MonkStateAnimation> animations;
        public string GetAnimation(UnitState unitState, AnimState animState)
        {
            var anim = animations.Find(a =>
                a.unitState == unitState
                && a.animState == animState);

            return anim?.animationName;
        }
    }
}