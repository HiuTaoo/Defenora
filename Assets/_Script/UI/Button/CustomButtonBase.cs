using UnityEngine;
using UnityEngine.EventSystems;

namespace _Script.UI.Button
{
    public abstract class CustomButtonBase: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
        }
    }
}