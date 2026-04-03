using _Script.Unit_Management_System.Animation.Profile_Script;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation
{
    public class AnimationFSM : MonoBehaviour
    {
        private Animator animator;

        [SerializeField] private ScriptableObject animationProfileSO;

        private IAnimationProfile animationProfile;
        private ScriptableObject runtimeProfile;

        private void Awake()
        {
            runtimeProfile = Instantiate(animationProfileSO);
            animationProfile = runtimeProfile as IAnimationProfile;
            animator = GetComponent<Animator>();
        }

        public void ChangeState(UnitState unitState, AnimState animState)
        {
            var anim = animationProfile.GetAnimation(unitState, animState);

            if (!string.IsNullOrEmpty(anim))
                animator.Play(anim);
        }

        public void SetTool(ToolType tool)
        {
            if (animationProfile is BuilderAnimProfile builderProfile)
                builderProfile.SetTool(tool);
        }

        public void SetResource(ResourceType resource)
        {
            if (animationProfile is BuilderAnimProfile builderProfile)
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

        public void SetWarriorDirection(WarriorDirection direction)
        {
            if (animationProfile is WarriorAnimProfile warriorAnimProfile)
            {
                warriorAnimProfile.SetWarriorDirection(direction);
            }
        }
        
        public WarriorDirection GetWarriorDirection()
        {
            if (animationProfile is WarriorAnimProfile warriorAnimProfile)
            {
                return warriorAnimProfile.currentWarriorDirection;
            }

            return WarriorDirection.None;
        }
    }
}