using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile_Script
{
    [CreateAssetMenu(menuName = "CustomAnimation/TNTGoblin Animation Profile")]
    public class TNTGoblinProfile: ScriptableObject, IAnimationProfile
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