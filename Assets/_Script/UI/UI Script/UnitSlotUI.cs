using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI.UI_Script 
{
    public class UnitSlotUI : MonoBehaviour
    {
        public Image unitIcon;
        public UnityEngine.UI.Button clickButton;
        
        private Unit currentUnit;

        private void Awake()
        {
            if (clickButton == null) 
                clickButton = GetComponent<UnityEngine.UI.Button>();
                
            clickButton.onClick.AddListener(OnSlotClicked);
        }

        public void Setup(Unit unit)
        {
            currentUnit = unit;
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
            if (currentUnit == null) return;

            SelectUnitSystem.Instance.selectedUnit = currentUnit.gameObject;
            SelectUnitSystem.Instance.targetBuilding = null; 

            SelectUnitSystem.Instance.OnSelectUnit?.Invoke(currentUnit.gameObject);

            SelectUnitSystem.Instance.OnLerpToSelectedUnit?.Invoke(currentUnit.transform.position);
        }
    }
}