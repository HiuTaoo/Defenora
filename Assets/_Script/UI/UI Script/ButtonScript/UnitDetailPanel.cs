using System.Collections.Generic;
using System.Globalization;
using _Script.UI.UI_Script.ButtonScript;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI.UI_Script
{
    public class UnitDetailPanel : MonoBehaviour
    {
        [Header("Basic Info Unit UI")] public GameObject unitpanel;

        public Image unitIcon;
        public TextMeshProUGUI unitNameText;
        public TextMeshProUGUI levelText;
        public GameObject upgradeButton;
        public Transform unitStatsContainer;

        [Header("Dynamic Stats UI")] public GameObject statPrefab;

        private Unit currentSelectedUnit;

        public void ShowUnitInfo(Unit unit)
        {
            if (currentSelectedUnit != null)
            {
                currentSelectedUnit.unitStatsManager.OnStatsUpdated -= UpdateUI;
                currentSelectedUnit.health.OnHealthChanged -= UpdateHealthUI;
            }

            currentSelectedUnit = unit;

            if (currentSelectedUnit == null)
            {
                unitpanel.SetActive(false);
                return;
            }

            unitpanel.SetActive(true);

            var baseData = currentSelectedUnit.unitStatsManager.GetBaseData();
            unitNameText.text = baseData.unitName;
            unitIcon.sprite = baseData.unitIcon;

            currentSelectedUnit.unitStatsManager.OnStatsUpdated += UpdateUI;
            currentSelectedUnit.health.OnHealthChanged += UpdateHealthUI;

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (currentSelectedUnit == null) return;

            levelText.text = "Level: " + currentSelectedUnit.unitStatsManager.currentLevel;

            var hpValue =
                $"{currentSelectedUnit.health.CurrentHealth}/{currentSelectedUnit.unitStatsManager.MaxHealth}";

            var statsList = new List<(string name, string value)>
            {
                ("HP", hpValue),
                ("View Distance",
                    currentSelectedUnit.unitStatsManager.ViewDistance.ToString(CultureInfo.InvariantCulture)),
                ("Speed", currentSelectedUnit.characterMovement.moveSpeed.ToString(CultureInfo.InvariantCulture))
            };

            var specialStats = currentSelectedUnit.GetSpecialStats();

            if (specialStats != null && specialStats.Count > 0) statsList.AddRange(specialStats);

            if (currentSelectedUnit.CompareTag("Enemy") || (currentSelectedUnit.CompareTag("NPC")
                                                            && currentSelectedUnit.unitStatsManager.IsMaxLevelUp()))
            {
                upgradeButton.SetActive(false);
            }
            else
            {
                upgradeButton.SetActive(true);
                var upgradeButtonScript = upgradeButton.GetComponent<UpgradeButton>();
                upgradeButtonScript.SetUpButton(currentSelectedUnit);
            }

            RenderDynamicStats(statsList);
        }

        private void UpdateHealthUI(float currentHp, float maxHp)
        {
            UpdateUI();
        }

        private void RenderDynamicStats(List<(string name, string value)> stats)
        {
            for (var i = unitStatsContainer.childCount - 1; i >= 0; i--)
            {
                var child = unitStatsContainer.GetChild(i);

                if (child.gameObject.activeSelf) PoolManager.Instance.Despawn(child.gameObject);
            }

            foreach (var stat in stats)
            {
                var obj = PoolManager.Instance.Spawn(statPrefab, unitStatsContainer.position, Quaternion.identity);

                obj.transform.SetParent(unitStatsContainer, false);
                obj.transform.localScale = Vector3.one;
                obj.transform.SetAsLastSibling();

                var statItem = obj.GetComponent<StatUIItem>();
                if (statItem != null) statItem.Setup(stat.name, stat.value);
            }
        }
    }
}