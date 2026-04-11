using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile_Script
{
    [CreateAssetMenu(menuName = "CustomAnimation/Pawn Animation Profile")]
    public class BuilderAnimProfile : ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class BuilderStateAnimation
        {
            public UnitState unitState;
            public AnimState animState;
            public ToolType tool;
            public ResourceType resource;
            public string animationName;
        }

        public List<BuilderStateAnimation> animations;

        private ResourceType currentResource = ResourceType.None;
        private ToolType currentTool = ToolType.None;

        public void SetResource(ResourceType resource)
        {
            currentResource = resource;
        }

        public void SetTool(ToolType newTool)
        {
            currentTool = newTool;
        }

        public string GetAnimation(UnitState unitState, AnimState animState)
        {
            var anim = animations.Find(a =>
                a.unitState == unitState &&
                a.animState == animState &&
                a.tool == currentTool &&
                a.resource == currentResource);

            return anim?.animationName;
        }
    }

}