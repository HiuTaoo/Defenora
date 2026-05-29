using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Data;
using _Script.Enum;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using _Script.Unit_Management_System.Building;
using UnityEngine;

public class Barrack : Building
{
    private class TrainingSlot
    {
        public Unit npcUnit;
        public float currentTrainingHours;
        public TrainingConfig trainingConfig; 

        public TrainingSlot(Unit unit, TrainingConfig config)
        {
            npcUnit = unit;
            currentTrainingHours = 0f;
            trainingConfig = config;
        }
    }

    [Header("Barrack Runtime Stats")]
    public int maxTraineeCapacity = 3;
    private TrainingConfig[] availableConfigs;

    [Header("Debug View (Read Only)")]
    [SerializeField] private int currentTraineeCount = 0;
    [SerializeField] private List<TraineeDebugEntry> debugTrainees = new List<TraineeDebugEntry>();

    private readonly List<TrainingSlot> trainingSlots = new List<TrainingSlot>();
    private float lastGameTime;

    public override void Awake()
    {
        base.Awake();

        if (configData is BarrackData barrackConfig)
        {
            maxTraineeCapacity = barrackConfig.maxTraineeCapacity;
            availableConfigs = barrackConfig.upgradeConfigs;
        }
        else
        {
            Debug.LogError($"[Barrack] {gameObject.name} yêu cầu Config Data phải là loại BarrackData!");
        }
    }

    private void Start()
    {
        if (TimeOfDaySystem.Instance != null)
        {
            lastGameTime = TimeOfDaySystem.Instance.GetCurrentTime();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (TimeOfDaySystem.Instance == null) return;

        float currentGameTime = TimeOfDaySystem.Instance.GetCurrentTime();
        float gameTimeDelta = currentGameTime - lastGameTime;

        if (gameTimeDelta < 0f)
        {
            gameTimeDelta = (24f - lastGameTime) + currentGameTime;
        }

        lastGameTime = currentGameTime;

        if (buildingState == BuildingState.Completed && trainingSlots.Count > 0)
        {
            ProcessTraining(gameTimeDelta);
        }
    }

    private void ProcessTraining(float gameTimeDelta)
    {
        // Hệ thống sẽ chỉ huấn luyện cho người đứng ở đầu hàng (index 0)
        // Khi người đầu tiên tốt nghiệp, người tiếp theo mới bắt đầu được tính giờ
        if (trainingSlots.Count > 0)
        {
            var activeSlot = trainingSlots[0];
            activeSlot.currentTrainingHours += gameTimeDelta;

            if (activeSlot.currentTrainingHours >= activeSlot.trainingConfig.trainingDurationInGameHours)
            {
                GraduateTrainee(activeSlot);
            }
        }

        SyncDebugView();
    }

    private void GraduateTrainee(TrainingSlot slot)
    {
        Unit unit = slot.npcUnit;
        unit.gameObject.SetActive(true);

        // Giải phóng Civilian khỏi hệ thống
        RemoveUnit(unit);
        UnitManager.Instance.allUnits.Remove(unit);
        PoolManager.Instance.Despawn(unit.gameObject);

        // Sinh ra Class lính mới dựa trên cấu hình cụ thể đã chọn của slot này
        var spawnPrefab = slot.trainingConfig.unitPrefab;
        if (spawnPrefab != null)
        {
            var newSoldier = PoolManager.Instance.Spawn(spawnPrefab, 
                GetRandomPositionAroundBuilding(), 
                Quaternion.identity);

            var unitComponent = newSoldier.GetComponent<Unit>();
            if (unitComponent != null)
            {
                UnitManager.Instance.RegisterUnit(unitComponent);
            }
        }

        SyncDebugView();
    }

    public override bool CanAddUnit(Unit unit)
    {
        if (unit.unitType == UnitType.Builder)
            return false;

        if (unit.unitType == UnitType.Civilian)
        {
            if (trainingSlots.Count >= maxTraineeCapacity)
                return false;

            return !trainingSlots.Exists(s => s.npcUnit == unit);
        }

        if (currentCapacity >= maxCapacity)
            return false;

        return !stationedUnits.Contains(unit);
    }

    /// <summary>
    /// Hàm nạp mặc định từ lớp cha hoặc kéo thả hệ thống (Mặc định chọn class đầu tiên trong danh sách cấu hình)
    /// </summary>
    public override void AddUnit(Unit unit)
    {
        if (unit.unitType == UnitType.Civilian)
        {
            // Mặc định lấy cấu hình đầu tiên nếu không chỉ định Class học
            TrainingConfig defaultConfig = (availableConfigs != null && availableConfigs.Length > 0) 
                ? availableConfigs[0] 
                : default;

            AddTrainee(unit, defaultConfig);
        }
        else
        {
            base.AddUnit(unit);
            unit.transform.position = GetRandomPositionAroundBuilding();
        }

        SyncDebugView();
    }

    /// <summary>
    /// Hàm bổ sung: Dùng khi người chơi mở UI bấm chọn chính xác Class muốn train
    /// </summary>
    public void AddTraineeWithSelection(Unit unit, UnitType targetType)
    {
        if (!CanAddUnit(unit)) return;

        // Tìm kiếm cấu hình tương ứng với loại lính người chơi chọn
        TrainingConfig selectedConfig = availableConfigs.FirstOrDefault(c => c.targetType == targetType);

        if (selectedConfig.unitPrefab == null)
        {
            Debug.LogError($"[Barrack] Chưa cấu hình Prefab cho loại lính {targetType} trong ScriptableObject!");
            return;
        }

        AddTrainee(unit, selectedConfig);
        SyncDebugView();
    }

    private void AddTrainee(Unit unit, TrainingConfig config)
    {
        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;

        trainingSlots.Add(new TrainingSlot(unit, config));
        currentTraineeCount = trainingSlots.Count;

        // Đặt ở vị trí của Barrack và ẩn đi giống Tu viện
        unit.transform.position = transform.position;
        unit.gameObject.SetActive(false);
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

                SyncDebugView();
                return true;
            }
            return false;
        }

        return base.RemoveUnit(unit);
    }

    protected override void HandleDeath()
    {
        foreach (var slot in trainingSlots)
        {
            slot.npcUnit.gameObject.SetActive(true);
            slot.npcUnit.currentState = UnitState.Idle;
            slot.npcUnit.assignedBuilding = null;
        }
        trainingSlots.Clear();
        currentTraineeCount = 0;

        SyncDebugView();
        base.HandleDeath();
    }

    private void SyncDebugView()
    {
        debugTrainees.Clear();
        foreach (var slot in trainingSlots)
        {
            if (slot != null && slot.npcUnit != null)
            {
                float flooredHours = Mathf.Floor(slot.currentTrainingHours * 10f) / 10f;
                string displayName = $"{slot.npcUnit.unitName} [{slot.trainingConfig.targetType}]";
                
                debugTrainees.Add(new TraineeDebugEntry
                {
                    unitName = displayName,
                    progress = $"{flooredHours:F1}h / {slot.trainingConfig.trainingDurationInGameHours:F1}h"
                });
            }
        }
    }
}