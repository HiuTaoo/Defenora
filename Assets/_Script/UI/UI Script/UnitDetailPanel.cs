using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI.UI_Script
{
    public class UnitDetailPanel: MonoBehaviour
    { 
        [Header("UI Elements")]
        public GameObject panel;
        public Image unitIcon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI attackText;
        public TextMeshProUGUI viewDistance;

        private Unit currentSelectedUnit;

        public void ShowUnitInfo(Unit unit)
        {
            // Nếu đang chọn unit cũ, hủy đăng ký event để tránh memory leak
            if (currentSelectedUnit != null)
            {
                currentSelectedUnit.statsManager.OnStatsUpdated -= UpdateUI;
                currentSelectedUnit.health.OnHealthChanged -= UpdateHealthUI;
            }

            currentSelectedUnit = unit;

            if (currentSelectedUnit == null)
            {
                panel.SetActive(false);
                return;
            }

            panel.SetActive(true);

            // Lấy dữ liệu SO để hiện tên và Icon
            UnitStatsSO baseData = currentSelectedUnit.statsManager.GetBaseData();
            nameText.text = baseData.unitName;
            unitIcon.sprite = baseData.unitIcon;

            // Đăng ký sự kiện
            currentSelectedUnit.statsManager.OnStatsUpdated += UpdateUI;
            currentSelectedUnit.health.OnHealthChanged += UpdateHealthUI;

            // Cập nhật UI lần đầu tiên
            UpdateUI();
            UpdateHealthUI(currentSelectedUnit.health.CurrentHealth, 
                currentSelectedUnit.statsManager.MaxHealth);
        }

        // Hàm này tự động chạy mỗi khi Unit lên level hoặc thay đổi chỉ số
        private void UpdateUI()
        {
            if (currentSelectedUnit == null) return;

            levelText.text = "Level: " + currentSelectedUnit.statsManager.currentLevel;
            attackText.text = currentSelectedUnit.statsManager.AttackDamage.ToString(CultureInfo.InvariantCulture);
            viewDistance.text = "" + currentSelectedUnit.statsManager.ViewDistance;
        }

        private void UpdateHealthUI(float currentHp, float maxHp)
        {
            hpText.text = $"{currentHp}/{maxHp}";
        }

        // Nút Level Up trên UI gọi hàm này
        public void Button_LevelUpClicked()
        {
            if (currentSelectedUnit != null)
            {
                currentSelectedUnit.statsManager.LevelUp();
            }
        }
    }
}