using _Script.UI.UI_Script;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrainingQueueSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image civilianIcon;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image targetClassIcon;
    [SerializeField] private TextMeshProUGUI remainingTimeText;

    private Unit assignedUnit;
    private TrainingBuilding currentBuilding;

    private float maxDuration;
    private UnitType targetType;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ConfirmDialog.Instance != null && assignedUnit != null && currentBuilding != null)
        {
            var question =
                $"Are you sure you want to cancel training of {assignedUnit.unitType} to {targetType}?  Consumed resources will not be refunded!";

            ConfirmDialog.Instance.Show(
                question,
                () =>
                {
                    currentBuilding.RemoveUnit(assignedUnit);

                    if (TrainingWindowUI.Instance != null)
                        TrainingWindowUI.Instance.ForceRefreshFullWindow();
                    else
                        PoolManager.Instance.Despawn(gameObject);
                },
                () => { }
            );
        }
    }

    public void SetupSlot(Unit unit, TrainingConfig config, TrainingBuilding building)
    {
        assignedUnit = unit;
        currentBuilding = building;
        targetType = config.targetType;
        maxDuration = config.trainingDurationInGameHours;

        if (unit != null && unit.unitStatsManager != null && unit.unitStatsManager.unitData != null)
            civilianIcon.sprite = unit.unitStatsManager.unitData.unitIcon;

        if (config.unitPrefab != null)
        {
            var unitComponent = config.unitPrefab.GetComponent<Unit>();

            if (unitComponent != null && unitComponent.unitStatsManager != null &&
                unitComponent.unitStatsManager.unitData != null)
            {
                targetClassIcon.sprite = unitComponent.unitStatsManager.unitData.unitIcon;
            }
            else
            {
                var statmanager = unitComponent.GetComponentInChildren<UnitStatsManager>();
                targetClassIcon.sprite = statmanager.unitData.unitIcon;
            }
        }

        if (progressSlider != null) progressSlider.value = 0f;

        if (remainingTimeText != null) remainingTimeText.text = $"{maxDuration:F1}h left";
    }

    public void UpdateSliderProgress(float currentHours)
    {
        if (maxDuration <= 0f) return;

        if (progressSlider != null)
        {
            var percentage = currentHours / maxDuration;
            progressSlider.value = percentage;
        }

        if (remainingTimeText != null)
        {
            var remainingHours = maxDuration - currentHours;

            if (remainingHours < 0f) remainingHours = 0f;

            var formattedHours = Mathf.Floor(remainingHours * 10f) / 10f;
            remainingTimeText.text = $"{formattedHours:F1}h left";

            // remainingTimeText.text = $"{remainingHours:F1} giờ nữa";
        }
    }
}