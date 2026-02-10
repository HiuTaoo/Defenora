using System.Collections.Generic;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation.Profile
{
    [CreateAssetMenu(menuName = "Animation/Pawn Animation Profile")]
    public class PawnAnimProfile : ScriptableObject, IAnimationProfile
    {
        [System.Serializable]
        public class StateAnimation
        {
            public UnitState state;
            public ToolType tool;
            public ResourceType resource;
            public string animationName;
        }

        public List<StateAnimation> animations;

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

        public string GetAnimation(UnitState state)
        {
            var anim = animations.Find(a =>
                a.state == state &&
                a.tool == currentTool &&
                a.resource == currentResource);

            return anim != null ? anim.animationName : null;
        }
    }

}