using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Enum;
using _Script.Object_Pooling;
using UnityEngine;

public class Monastery : TrainingBuilding
{
    protected override TrainingConfig[] GetUpgradeConfigs()
    {
        var data = configData as MonasteryData;
        if (data == null) return null;

        return new TrainingConfig[] {
            new TrainingConfig {
                targetType = UnitType.Monk,
                trainingDurationInGameHours = data.trainingDurationInGameHours,
                unitPrefab = PrefabConfig.Instance.monkPrefab,
                trainingCosts = data.trainingCosts 
            }
        };
    }

    protected override int GetMaxTraineeCapacity() => (configData as MonasteryData)?.maxTraineeCapacity ?? 2;

    public override void AddTraineeWithSelection(Unit unit, UnitType targetType)
    {
        TrainingConfig selectedConfig = availableConfigs.FirstOrDefault(c => c.targetType == targetType);
        if (selectedConfig.unitPrefab == null) return;

        if (!HasEnoughResources(selectedConfig))
        {
            Debug.LogWarning($"[Monastery] Không đủ tài nguyên để tu hành thành {targetType}!");
            return; 
        }

        if (!CanAddUnit(unit)) return;

        if (selectedConfig.trainingCosts != null && Inventory.Instance != null)
        {
            foreach (var cost in selectedConfig.trainingCosts)
            {
                if (cost.itemData == null || cost.amount <= 0) continue;
                Inventory.Instance.Remove(cost.itemData, cost.amount);
                Debug.Log($"[Monastery] Đã khấu trừ {cost.amount}x {cost.itemData.name} từ kho tổng.");
            }
        }

        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;

        trainingSlots.Add(new TrainingSlot(unit, selectedConfig));
        currentTraineeCount = trainingSlots.Count;

        unit.transform.position = transform.position;
        unit.gameObject.SetActive(false);

        SyncDebugView();
    }

    public override bool RemoveUnit(Unit unit)
    {
        if (unit.unitType == UnitType.Civilian)
        {
            int index = trainingSlots.FindIndex(s => s.npcUnit == unit);
            if (index >= 0)
            {
                var slotToCancel = trainingSlots[index];

                if (slotToCancel.trainingConfig.trainingCosts != null && Inventory.Instance != null)
                {
                    foreach (var cost in slotToCancel.trainingConfig.trainingCosts)
                    {
                        if (cost.itemData == null || cost.amount <= 0) continue;
                        Inventory.Instance.Add(cost.itemData, cost.amount);
                        Debug.Log($"[Monastery] Hoàn trả {cost.amount}x {cost.itemData.name} do hủy tu hành giữa chừng.");
                    }
                }

                trainingSlots.RemoveAt(index);
                currentTraineeCount = trainingSlots.Count;

                unit.gameObject.SetActive(true);
                unit.currentState = UnitState.Idle;
                unit.assignedBuilding = null;
                unit.transform.position = GetRandomPositionAroundBuilding();

                SyncDebugView();
                return true;
            }
            return false;
        }

        return base.RemoveUnit(unit);
    }
}