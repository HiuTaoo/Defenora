using System;
using UnityEngine;

namespace _Script.UI.UI_Script.ButtonScript
{
    public class UpgradeButton: MonoBehaviour
    {
        private Unit currentUnit;
        private UnityEngine.UI.Button upgradeButton;

        private void Awake()
        {
            upgradeButton = GetComponent<UnityEngine.UI.Button>();
            upgradeButton.onClick.AddListener(LevelUpUnit);
        }

        public void SetUpButton(Unit unit)
        {
            currentUnit = unit;
        }

        private void LevelUpUnit()
        {
            currentUnit.unitStatsManager.LevelUp();
        }
    }
}