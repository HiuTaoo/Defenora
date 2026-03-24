using _Script.Unit_Management_System.Animation.Profile_Script;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation
{
    public class AnimationFSM : MonoBehaviour
    {
        private Animator animator;

        [SerializeField] private ScriptableObject animationProfileSO;

        private IAnimationProfile animationProfile;

        private void Awake()
        {
            animationProfile = animationProfileSO as IAnimationProfile;
            animator = GetComponent<Animator>();
        }

        public void ChangeState(UnitState state)
        {
            var anim = animationProfile.GetAnimation(state);

            if (!string.IsNullOrEmpty(anim))
                animator.Play(anim);
        }

        public void SetTool(ToolType tool)
        {
            if(animationProfile is BuilderAnimProfile builderProfile)
                builderProfile.SetTool(tool);
        }

        public void SetResource(ResourceType resource)
        {
            if(animationProfile is BuilderAnimProfile builderProfile)
                builderProfile.SetResource(resource);
        }

        public void SetFireDirection(ArcherFireDirection direction)
        {
            if (animationProfile is ArcherAnimProfile archerAnimProfile)
            {
                archerAnimProfile.SetCurrentFireDirection(direction);
            }
        }

        public void SetLancerDirection(LancerDirection direction)
        {
            if (animationProfile is LancerAnimProfile lancerAnimProfile)
            {
                lancerAnimProfile.SetLancerDirection(direction);
            }
        }

        public LancerDirection GetLancerDirection()
        {
            if (animationProfile is LancerAnimProfile lancerAnimProfile)
            {
                return lancerAnimProfile.currentLancerDirection;
            }

            return LancerDirection.None;
        }
    }
}