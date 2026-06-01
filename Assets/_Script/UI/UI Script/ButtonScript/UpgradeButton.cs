using System;
using UnityEngine;

namespace _Script.UI.UI_Script.ButtonScript
{
    public class UpgradeButton : MonoBehaviour, IPoolable
    {
        private Unit currentUnit;
        private UnityEngine.UI.Button upgradeButton;

        private void Awake()
        {
            upgradeButton = GetComponent<UnityEngine.UI.Button>();
        }

        public void SetUpButton(Unit unit)
        {
            currentUnit = unit;
        }

        private void LevelUpUnit()
        {
            if (currentUnit != null && currentUnit.unitStatsManager != null)
            {
                currentUnit.unitStatsManager.LevelUp();
            }
        }

        /// <summary>
        /// Được gọi ngay khi Nút Nâng Cấp được lôi ra khỏi Pool
        /// </summary>
        public void OnSpawned()
        {
            if (upgradeButton == null) 
                upgradeButton = GetComponent<UnityEngine.UI.Button>();

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                
                upgradeButton.interactable = true;
                
                upgradeButton.onClick.AddListener(LevelUpUnit);
            }

            currentUnit = null;
        }

        /// <summary>
        /// Được gọi ngay trước khi Nút Nâng Cấp bị cất vào ngầm trong Pool
        /// </summary>
        public void OnDespawned()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
            }

            currentUnit = null;
        }
    }
}