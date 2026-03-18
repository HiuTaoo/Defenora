using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile_Script
{
    [CreateAssetMenu(menuName = "Animation/Archer Animation Profile")]
    public class ArcherAnimProfile: ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class ArcherStateAnimation
        {
            public UnitState state;
            public ArcherFireDirection fireDirection;
            public string animationName;
        }

        public List<ArcherStateAnimation> animations;
        public ArcherFireDirection currentFireDirection {get; private set;}
        public string GetAnimation(UnitState state)
        {
            var anim = animations.Find(a =>
                a.state == state &&
                a.fireDirection == currentFireDirection);

            return anim?.animationName;
        }

        public void SetCurrentFireDirection(ArcherFireDirection dir)
        {
            currentFireDirection = dir;
        }
        
    }
}