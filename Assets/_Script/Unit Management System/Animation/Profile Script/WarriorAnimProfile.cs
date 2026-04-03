using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile_Script
{
    [CreateAssetMenu(menuName = "Animation/Warrior Animation Profile")]
    public class WarriorAnimProfile: ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class WarriorStateAnimation
        {
            public UnitState unitState;
            public AnimState animState;
            public WarriorDirection  direction;
            public List<string> animationName;
        }

        public List<WarriorStateAnimation> animations;
        public WarriorDirection currentWarriorDirection {get; private set;}
        public string GetAnimation(UnitState unitState, AnimState animState)
        {
            var anim = animations.Find(a 
                => a.unitState == unitState
                   && a.animState == animState
                   && a.direction == currentWarriorDirection);

            if (anim == null || anim.animationName == null || anim.animationName.Count == 0)
                return null;

            int index = Random.Range(0, anim.animationName.Count);
            return anim.animationName[index];
        }

        public void SetWarriorDirection(WarriorDirection direction)
        {
            currentWarriorDirection = direction;
        }
        
    }
}