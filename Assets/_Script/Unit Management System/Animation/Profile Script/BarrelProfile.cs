using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile_Script
{
    [CreateAssetMenu(menuName = "CustomAnimation/Barrel Animation Profile")]
    public class BarrelProfile: ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class StateAnimation
        {
            public UnitState unitState;
            public AnimState animState;
            public string animationName;
        }

        public List<StateAnimation> animations;

        public string GetAnimation(UnitState unitState, AnimState animState)
        {
            var anim = animations.Find(
                a => a.unitState == unitState 
                     && a.animState == animState);
            return anim?.animationName;
        }
    }
}