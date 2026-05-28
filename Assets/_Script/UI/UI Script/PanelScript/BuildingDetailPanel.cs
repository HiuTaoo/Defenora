using System.Collections.Generic;
using System.Globalization;
using _Script.Object_Pooling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI.UI_Script
{
    public class BuildingDetailPanel : MonoBehaviour
    {
        [Header("Basic Info Building UI")]
        public GameObject buildingPanel;
        public TextMeshProUGUI buildingNameText;

        [Header("Capacity UI")]
        public TextMeshProUGUI capacityText;

        [Header("Dynamic Stats UI")]
        public Transform buildingStatsContainer;
        public Transform capacityDetailContainer;


        private Building currentSelectedBuilding;

        private void Update()
        {
            if (currentSelectedBuilding != null && currentSelectedBuilding.buildingState == BuildingState.UnderConstruction)
            {
                UpdateUI(); 
            }
        }

        public void ShowBuildingInfo(Building building)
        {
            if (currentSelectedBuilding != null)
            {
                if (currentSelectedBuilding.health != null)
                    currentSelectedBuilding.health.OnHealthChanged -= UpdateHealthUI;
                currentSelectedBuilding.OnStationedUnitsChanged -= RefreshUnitList;
            }

            currentSelectedBuilding = building;

            if (currentSelectedBuilding == null)
            {
                buildingPanel.SetActive(false);
                return;
            }

            buildingPanel.SetActive(true);
            buildingNameText.text = currentSelectedBuilding.buildingType.ToString();

            if (currentSelectedBuilding.health != null)
            {
                currentSelectedBuilding.health.OnHealthChanged += UpdateHealthUI;
            }

            currentSelectedBuilding.OnStationedUnitsChanged += RefreshUnitList;
            
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (currentSelectedBuilding == null) return;

            if (capacityText != null)
            {
                capacityText.text = $"Capacity: {currentSelectedBuilding.currentCapacity}/{currentSelectedBuilding.maxCapacity}";
            }

            string hpValue = "0/0";
            if (currentSelectedBuilding.health != null)
            {
                hpValue = $"{Mathf.RoundToInt(currentSelectedBuilding.health.CurrentHealth)}/{Mathf.RoundToInt(currentSelectedBuilding.health.maxHealth)}";
            }

            var statsList = new List<(string name, string value)>
            {
                ("HP", hpValue),
                ("Max Capacity", currentSelectedBuilding.maxCapacity.ToString(CultureInfo.InvariantCulture))
            };

            RenderDynamicStats(statsList);
            
            // GỌI HÀM RENDER ICON LÍNH Ở ĐÂY
            RenderStationedUnits();
        }

        private void UpdateHealthUI(float currentHp, float maxHp)
        {
            UpdateUI();
        }

        private void RenderDynamicStats(List<(string name, string value)> stats)
        {
            for (int i = buildingStatsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = buildingStatsContainer.GetChild(i);
                
                if (child.gameObject.activeSelf) 
                {
                    PoolManager.Instance.Despawn(child.gameObject); 
                }
            }

            foreach (var stat in stats)
            {
                GameObject obj = PoolManager.Instance.Spawn(PrefabConfig.Instance.statDetailGUIPrefab, buildingStatsContainer.position, Quaternion.identity);
                
                obj.transform.SetParent(buildingStatsContainer, false);
                obj.transform.localScale = Vector3.one;
                obj.transform.SetAsLastSibling(); 

                StatUIItem statItem = obj.GetComponent<StatUIItem>();
                if (statItem != null)
                {
                    statItem.Setup(stat.name, stat.value);
                }
            }
        }

        private void RenderStationedUnits()
        {
            if (capacityDetailContainer == null || PrefabConfig.Instance.unitIconPrefab == null) return;

            for (int i = capacityDetailContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = capacityDetailContainer.GetChild(i);
                if (child.gameObject.activeSelf) 
                {
                    PoolManager.Instance.Despawn(child.gameObject); 
                }
            }

            if (currentSelectedBuilding.stationedUnits == null)
                return;

            foreach (var unit in currentSelectedBuilding.stationedUnits)
            {
                GameObject obj = PoolManager.Instance.Spawn(PrefabConfig.Instance.unitIconPrefab, capacityDetailContainer.position, Quaternion.identity);
                
                obj.transform.SetParent(capacityDetailContainer, false);
                obj.transform.localScale = Vector3.one;
                obj.transform.SetAsLastSibling(); 

                UnitSlotUI slotUI = obj.GetComponent<UnitSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(unit, FocusUnitCamera);
                }
            }

            if (currentSelectedBuilding.stationedUnits.Count < currentSelectedBuilding.maxCapacity && currentSelectedBuilding.buildingState == BuildingState.Completed)
            {
                GameObject obj = PoolManager.Instance.Spawn(PrefabConfig.Instance.addUnitButtonPrefab, capacityDetailContainer.position, Quaternion.identity);
                
                obj.transform.SetParent(capacityDetailContainer, false);
                obj.transform.localScale = Vector3.one;
                obj.transform.SetAsLastSibling(); 
            }
        }
        
        private void FocusUnitCamera(Unit clickedUnit)
        {
            /*SelectUnitSystem.Instance.selectedUnit = clickedUnit.gameObject;
            SelectUnitSystem.Instance.targetBuilding = null; 
            SelectUnitSystem.Instance.OnSelectUnit?.Invoke(clickedUnit.gameObject);*/
            SelectUnitSystem.Instance.OnLerpToSelectedUnit?.Invoke(clickedUnit.transform.position);
            UIManager.Instance.availableUnitPanel.availableUnitPanel.SetActive(false);
        }
        
        private void RefreshUnitList()
        {
            if (capacityText != null)
            {
                capacityText.text = $"Capacity: {currentSelectedBuilding.currentCapacity}/{currentSelectedBuilding.maxCapacity}";
            }
    
            RenderStationedUnits();
        }
    }
}