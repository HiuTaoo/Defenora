using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Script.UI.Button
{
    public class CustomButtonScale: CustomButtonBase
    {
        private const float OriginalScale = 1;
        [SerializeField] private float toScale;
        [SerializeField] private float duration;
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            transform.DOKill(); 
            transform.DOScale(toScale, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true); 
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            transform.DOKill();
            transform.DOScale(OriginalScale, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true); 
        }
    }
    
}