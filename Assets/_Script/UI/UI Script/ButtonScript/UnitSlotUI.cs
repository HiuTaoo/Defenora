using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

namespace _Script.UI.UI_Script 
{
    public class UnitSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private bool isPressed = false;
        
        public float holdRequiredTime = 1f; 
        private float currentHoldTimer = 0f; 
        private bool hasTriggeredHold = false; 

        public Image unitIcon;
        public UnityEngine.UI.Button clickButton;
        
        private Unit currentUnit;
        private Action<Unit> onClickCallback; 

        private void Awake()
        {
            if (clickButton == null) 
                clickButton = GetComponent<UnityEngine.UI.Button>();
                
            clickButton.onClick.AddListener(OnSlotClicked);
        }

        public void Setup(Unit unit, Action<Unit> actionWhenClicked)
        {
            currentUnit = unit;
            onClickCallback = actionWhenClicked; 

            if (unit != null && unit.statsManager != null)
            {
                var baseData = unit.statsManager.GetBaseData();
                if (baseData != null)
                {
                    unitIcon.sprite = baseData.unitIcon;
                }
            }
        }

        private void OnSlotClicked()
        {
            // Tùy chọn: Nếu bạn không muốn kích hoạt sự kiện click bình thường 
            // khi người chơi đã nhấn giữ đủ 2 giây, hãy bỏ comment dòng dưới đây:
            // if (hasTriggeredHold) return; 

            if (currentUnit == null) return;
            onClickCallback?.Invoke(currentUnit); 
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            
            currentHoldTimer = 0f;
            hasTriggeredHold = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            
            currentHoldTimer = 0f;
        }

        private void Update()
        {
            if (isPressed && !hasTriggeredHold)
            {
                currentHoldTimer += Time.deltaTime;

                if (currentHoldTimer >= holdRequiredTime)
                {
                    OnHoldAction(); 
                    hasTriggeredHold = true; 
                }
            }
        }

        private void OnHoldAction()
        {
            if(currentUnit == null || currentUnit.assignedBuilding == null) return;
            currentUnit.assignedBuilding.RemoveUnit(currentUnit);
        }
    }
}