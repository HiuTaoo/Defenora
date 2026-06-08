using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Script.UI.UI_Script
{
    public class UnitSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPoolable
    {
        public float holdRequiredTime = 1f;

        public Image unitIcon;
        public UnityEngine.UI.Button clickButton;
        private float currentHoldTimer;

        private Unit currentUnit;
        private bool hasTriggeredHold;
        private bool isPressed;
        private Action<Unit> onClickCallback;

        private void Awake()
        {
            if (clickButton == null)
                clickButton = GetComponent<UnityEngine.UI.Button>();
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

        public void Setup(Unit unit, Action<Unit> actionWhenClicked)
        {
            currentUnit = unit;
            onClickCallback = actionWhenClicked;

            if (unit != null)
            {
                if (unit.unitStatsManager == null)
                    unit.unitStatsManager = unit.GetComponentInChildren<UnitStatsManager>();

                var baseData = unit.unitStatsManager.GetBaseData();
                if (baseData != null) unitIcon.sprite = baseData.unitIcon;
            }
        }

        private void OnSlotClicked()
        {
            if (hasTriggeredHold) return;
            if (currentUnit == null) return;

            // 1. PHÁT SFX KHI CLICK CHỌN UNIT
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(SoundNames.SfxClick); // Đọc từ AudioManager gốc

            onClickCallback?.Invoke(currentUnit);
        }

        private void OnHoldAction()
        {
            if (currentUnit == null || currentUnit.assignedBuilding == null) return;

            // 2. PHÁT SFX KHI ẤN GIỮ THÀNH CÔNG (Cảnh báo mở Dialog)
            if (AudioManager.Instance != null)
                // Bạn có thể dùng chung SFX_Click hoặc thay bằng SFX hằng số khác như SFX_Warning/SFX_Alert nếu muốn
                AudioManager.Instance.PlaySFX(SoundNames.SfxClick);

            var str = $"Do you want to remove {currentUnit.unitType} from {currentUnit.assignedBuilding.buildingType}?";
            ConfirmDialog.Instance.Show(str, RemoveUnit);
        }

        private void RemoveUnit()
        {
            // 3. (Tùy chọn) PHÁT SFX KHI THỰC SỰ BẤM XÓA UNIT THÀNH CÔNG
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundNames.SfxClick);

            if (currentUnit != null && currentUnit.assignedBuilding != null)
            {
                currentUnit.assignedBuilding.RemoveUnit(currentUnit);
            }
        }

        /// <summary>
        /// Được gọi ngay khi Ô Slot này được lấy ra khỏi Pool
        /// </summary>
        public void OnSpawned()
        {
            if (clickButton == null) 
                clickButton = GetComponent<UnityEngine.UI.Button>();

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.interactable = true; 
                clickButton.onClick.AddListener(OnSlotClicked); 
            }

            isPressed = false;
            hasTriggeredHold = false;
            currentHoldTimer = 0f;

            currentUnit = null;
            onClickCallback = null;
            
            if (unitIcon != null) 
                unitIcon.sprite = null;
        }

        /// <summary>
        /// Được gọi ngay trước khi Ô Slot này bị ẩn và thu hồi về Pool
        /// </summary>
        public void OnDespawned()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
            }

            isPressed = false;
            hasTriggeredHold = false;
            currentHoldTimer = 0f;

            currentUnit = null;
            onClickCallback = null;
        }
    }
}