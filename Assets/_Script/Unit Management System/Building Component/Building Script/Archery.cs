using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Data;
using _Script.Enum;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using UnityEngine;

public class Archery : TrainingBuilding
{
    [Header("Archery Specific Runtime Stats")]
    public float trainingDurationInGameHours = 3f;

    [Header("Spot Reference")]
    [SerializeField] private GameObject traineeSpot;

    #region Đồng bộ dữ liệu cấu hình ban đầu lên Base Class
    protected override TrainingConfig[] GetUpgradeConfigs()
    {
        var data = configData as ArcheryData;
        if (data == null) return null;

        // Chuyển cấu hình đơn lẻ kiểu cũ của Archery thành cấu hình chung để UI đọc được
        return new TrainingConfig[] {
            new TrainingConfig {
                targetType = UnitType.Archer,
                trainingDurationInGameHours = data.trainingDuration,
                unitPrefab = PrefabConfig.Instance.archerPrefab
            }
        };
    }

    protected override int GetMaxTraineeCapacity() => (configData as ArcheryData)?.maxTraineeCapacity ?? 3;
    #endregion

    public override void Awake()
    {
        base.Awake(); 

        if (configData is ArcheryData archeryConfig)
        {
            trainingDurationInGameHours = archeryConfig.trainingDuration;
            maxTraineeCapacity = archeryConfig.maxTraineeCapacity;
        }
    }

    // 🌟 OVERRIDE: Giữ nguyên logic tính giờ SONG SONG cho toàn bộ danh sách trong hàng chờ
    protected override void ProcessTraining(float gameTimeDelta)
    {
        for (int i = trainingSlots.Count - 1; i >= 0; i--)
        {
            var slot = trainingSlots[i];
            slot.currentTrainingHours += gameTimeDelta;

            if (slot.currentTrainingHours >= trainingDurationInGameHours)
            {
                GraduateTrainee(slot);
            }
        }

        SyncDebugView();
    }

    // 🌟 OVERRIDE: Xử lý tốt nghiệp sinh ra Archer
    protected override void GraduateTrainee(TrainingSlot slot)
    {
        Unit unit = slot.npcUnit;
        unit.gameObject.SetActive(true);

        RemoveUnit(unit);
        UnitManager.Instance.allUnits.Remove(unit);
        PoolManager.Instance.Despawn(unit.gameObject);

        var archer = PoolManager.Instance.Spawn(PrefabConfig.Instance.archerPrefab, 
            GetRandomPositionAroundBuilding(),
            Quaternion.identity);
        var archerComponent = archer.GetComponent<Archer>();
        if (archerComponent != null)
        {
            UnitManager.Instance.RegisterUnit(archerComponent);
        }
        
        SyncDebugView();
    }

    // 🌟 OVERRIDE: Nạp học viên - Người đầu tiên xuất hiện ở traineeSpot, người sau bị ẩn
    public override void AddUnit(Unit unit)
    {
        if (unit.unitType == UnitType.Civilian)
        {
            unit.floorAgent.MoveToFloor(LayerIndex);
            unit.assignedBuilding = this;
            
            // Lấy cấu hình mặc định (Archer)
            TrainingConfig config = availableConfigs != null && availableConfigs.Length > 0 ? availableConfigs[0] : default;
            trainingSlots.Add(new TrainingSlot(unit, config));
            currentTraineeCount = trainingSlots.Count;

            if (trainingSlots.Count == 1)
            {
                unit.gameObject.SetActive(true);
                if (traineeSpot != null)
                    unit.transform.position = traineeSpot.transform.position;
                else
                    unit.transform.position = GetRandomPositionAroundBuilding();
            }
            else
            {
                unit.gameObject.SetActive(false);
            }
        }
        else
        {
            base.AddUnit(unit); 
            Vector3 availableSpot = GetRandomPositionAroundBuilding();
            unit.transform.position = availableSpot;
        }

        SyncDebugView();
    }

    // Cổng phụ kết nối luồng click UI
    public override void AddTraineeWithSelection(Unit unit, UnitType targetType)
    {
        AddUnit(unit);
    }

    public override bool RemoveUnit(Unit unit)
    {
        if (unit.unitType == UnitType.Civilian)
        {
            int index = trainingSlots.FindIndex(s => s.npcUnit == unit);
            if (index >= 0)
            {
                trainingSlots.RemoveAt(index);
                currentTraineeCount = trainingSlots.Count;
                
                unit.gameObject.SetActive(true);
                unit.currentState = UnitState.Idle;
                unit.assignedBuilding = null;
                unit.transform.position = GetRandomPositionAroundBuilding();

                if (index == 0 && trainingSlots.Count > 0)
                {
                    Unit nextTrainee = trainingSlots[0].npcUnit;
                    nextTrainee.gameObject.SetActive(true);
                    
                    if (traineeSpot != null)
                        nextTrainee.transform.position = traineeSpot.transform.position;
                }

                SyncDebugView();
                return true;
            }
            return false;
        }

        return base.RemoveUnit(unit);
    }

    public override void ForceAddTraineeOnLoad(Unit unit, float savedHours, UnitType targetType)
    {
        TrainingConfig config = availableConfigs != null && availableConfigs.Length > 0 ? availableConfigs[0] : default;
        var newSlot = new TrainingSlot(unit, config);
        newSlot.currentTrainingHours = savedHours;
        
        trainingSlots.Add(newSlot);
        currentTraineeCount = trainingSlots.Count;

        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;

        if (trainingSlots.Count == 1)
        {
            unit.gameObject.SetActive(true);
            if (traineeSpot != null)
                unit.transform.position = traineeSpot.transform.position;
        }
        else
        {
            unit.gameObject.SetActive(false);
        }

        SyncDebugView();
    }

    public override List<TraineeSaveData> GetTraineesSaveData()
    {
        return trainingSlots.Select(slot => new TraineeSaveData
        {
            unitID = slot.npcUnit.GetId(),
            currentTrainingHours = slot.currentTrainingHours,
            targetType = UnitType.Archer 
        }).ToList();
    }

    // 🌟 OVERRIDE: Đồng bộ giao diện Inspector (Giữ định dạng chỉ hiện thương hiệu tên gốc)
    protected override void SyncDebugView()
    {
        debugTrainees.Clear();
        foreach (var slot in trainingSlots)
        {
            if (slot != null && slot.npcUnit != null)
            {
                float flooredHours = Mathf.Floor(slot.currentTrainingHours * 10f) / 10f;

                debugTrainees.Add(new TraineeDebugEntry
                {
                    unitName = slot.npcUnit.unitName,
                    progress = $"{flooredHours:F1}h / {trainingDurationInGameHours:F1}h"
                });
            }
        }
    }
}


[System.Serializable]
public struct TraineeDebugEntry
{
    public string unitName;
    public string progress; 
}