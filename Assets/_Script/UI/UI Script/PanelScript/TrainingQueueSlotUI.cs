using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using _Script.UI.UI_Script;
using _Script.Enum;
using TMPro; 

public class TrainingQueueSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image civilianIcon;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image targetClassIcon;
    [SerializeField] private TextMeshProUGUI remainingTimeText; 

    private Unit assignedUnit;
    private TrainingBuilding currentBuilding;
    private UnitType targetType;

    private float maxDuration; 

    public void SetupSlot(Unit unit, TrainingConfig config, TrainingBuilding building)
    {
        assignedUnit = unit;
        currentBuilding = building;
        targetType = config.targetType;
        maxDuration = config.trainingDurationInGameHours;

        if (unit != null && unit.statsManager != null && unit.statsManager.unitData != null)
        {
            civilianIcon.sprite = unit.statsManager.unitData.unitIcon;
        }

        if (config.unitPrefab != null)
        {
            var unitComponent = config.unitPrefab.GetComponent<Unit>();
            
            if (unitComponent != null && unitComponent.statsManager != null && unitComponent.statsManager.unitData != null)
            {
                targetClassIcon.sprite = unitComponent.statsManager.unitData.unitIcon;
            }
            else
            {
                var statmanager = unitComponent.GetComponentInChildren<UnitStatsManager>();
                targetClassIcon.sprite = statmanager.unitData.unitIcon;
            }
        }
        
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }

        if (remainingTimeText != null)
        {
            remainingTimeText.text = $"{maxDuration:F1}h left";
        }
    }

    public void UpdateSliderProgress(float currentHours)
    {
        if (maxDuration <= 0f) return;

        if (progressSlider != null)
        {
            float percentage = currentHours / maxDuration;
            progressSlider.value = percentage;
        }

        if (remainingTimeText != null)
        {
            float remainingHours = maxDuration - currentHours;

            if (remainingHours < 0f) remainingHours = 0f;

            float formattedHours = Mathf.Floor(remainingHours * 10f) / 10f;
            remainingTimeText.text = $"{formattedHours:F1}h left";

            // remainingTimeText.text = $"{remainingHours:F1} giờ nữa";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ConfirmDialog.Instance != null && assignedUnit != null && currentBuilding != null)
        {
            string question = $"Bạn có chắc chắn muốn hủy quá trình huấn luyện của {assignedUnit.unitName} không?";
            
            ConfirmDialog.Instance.Show(
                question, 
                onYes: () => 
                {
                    currentBuilding.RemoveUnit(assignedUnit);
                    
                    if (TrainingWindowUI.Instance != null)
                    {
                        TrainingWindowUI.Instance.ForceRefreshFullWindow();
                    }
                    else
                    {
                        PoolManager.Instance.Despawn(gameObject);
                    }
                },
                onNo: () =>
                {
                }
            );
        }
    }
}