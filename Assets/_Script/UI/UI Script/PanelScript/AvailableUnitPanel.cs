using System;
using System.Collections.Generic;
using _Script.Object_Pooling;
using TMPro;
using UnityEngine;

namespace _Script.UI.UI_Script.PanelScript
{
    public class AvailableUnitPanel: MonoBehaviour
    {
        public GameObject availableUnitPanel;
        public Transform availableUnitContainer;
        public TextMeshProUGUI noticeText;
        
        private Building currentSelectedBuilding;
        private List<Unit> availableUnits;

        public void ShowAvailableUnitInfo(Building building)
        {
            if (currentSelectedBuilding != null)
            {
                currentSelectedBuilding.OnStationedUnitsChanged -= RefreshAvailableUnitList;
            }
            
            currentSelectedBuilding = building;

            if (currentSelectedBuilding == null)
            {
                availableUnitPanel.SetActive(false);
                return;
            }

            availableUnitPanel.SetActive(true);
            currentSelectedBuilding.OnStationedUnitsChanged += RefreshAvailableUnitList;
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (currentSelectedBuilding == null) return;
            RenderAvailableUnits();
        }

        private void RenderAvailableUnits()
        {
            if (availableUnitContainer != null)
            {
                for (int i = availableUnitContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = availableUnitContainer.GetChild(i);
                    if (child.gameObject.activeSelf) 
                    {
                        PoolManager.Instance.Despawn(child.gameObject); 
                    }
                }
            }
            
            noticeText.gameObject.SetActive(false);
            availableUnits = UnitManager.Instance.GetAvailableUnits();
            
            if (availableUnits == null || availableUnits.Count == 0 || PrefabConfig.Instance.unitIconPrefab == null)
            {
                noticeText.gameObject.SetActive(true);
                noticeText.text = "No Available Units Now";
                return;
            }

            foreach (var unit in availableUnits)
            {
                if (!currentSelectedBuilding.CanAddUnit(unit))
                    continue;

                GameObject obj = PoolManager.Instance.Spawn(PrefabConfig.Instance.unitIconPrefab, availableUnitContainer.position, Quaternion.identity);
                
                obj.transform.SetParent(availableUnitContainer, false);
                obj.transform.localScale = Vector3.one;
                obj.transform.SetAsLastSibling(); 

                UnitSlotUI slotUI = obj.GetComponent<UnitSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(unit, ShowConfirmDialog);
                }
            }
        }

        private void ShowConfirmDialog(Unit clickedUnit)
        {
            var str = $"Do you want to add {clickedUnit.unitType} to {currentSelectedBuilding.buildingType}?";
    
            // Dùng () => để tạo một hàm ẩn danh (không tham số) bọc cái hàm có tham số của bạn lại
            ConfirmDialog.Instance.Show(str, () => AssignUnitToBuilding(clickedUnit));
        }

        private void AssignUnitToBuilding(Unit clickedUnit)
        {
            if (currentSelectedBuilding != null && currentSelectedBuilding.CanAddUnit(clickedUnit))
            {
                currentSelectedBuilding.AddUnit(clickedUnit);
                RenderAvailableUnits();
                //availableUnitPanel.SetActive(false); 
            }
        }

        private void RefreshAvailableUnitList()
        {
            RenderAvailableUnits();
        }

        private void OnDisable()
        {
            if (noticeText != null) noticeText.gameObject.SetActive(false);
        }
        
    }
}