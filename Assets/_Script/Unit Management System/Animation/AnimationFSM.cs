using _Script.Unit_Management_System.Animation.Profile;
using UnityEngine;

namespace _Script.Unit_Management_System.Animation
{
    public class AnimationFSM : MonoBehaviour
    {
        private Animator animator;
        [SerializeField] private ScriptableObject animationProfileSO;

        private IAnimationProfile animationProfile;

        private PawnAnimProfile pawnProfile;

        private void Awake()
        {
            animationProfile = animationProfileSO as IAnimationProfile;
            pawnProfile = animationProfileSO as PawnAnimProfile;
            animator = GetComponent<Animator>();
        }

        public void ChangeState(UnitState state)
        {
            string anim = animationProfile.GetAnimation(state);
            if (!string.IsNullOrEmpty(anim))
                animator.Play(anim);
        }

        public void SetTool(ToolType tool)
        {
            if (pawnProfile != null)
                pawnProfile.SetTool(tool);
        }

        public void SetResource(ResourceType resource)
        {
            if (pawnProfile != null)
                pawnProfile.SetResource(resource);
        }
    }

}