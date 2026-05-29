using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Data;
using _Script.Enum;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using UnityEngine;

public abstract class TrainingBuilding : Building
{
    [System.Serializable]
    protected class TrainingSlot
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

    [Header("Training Runtime Stats")]
    public int maxTraineeCapacity = 3;
    protected TrainingConfig[] availableConfigs;

    [Header("Debug View (Read Only)")]
    [SerializeField] protected int currentTraineeCount = 0;
    [SerializeField] protected List<TraineeDebugEntry> debugTrainees = new List<TraineeDebugEntry>();

    protected readonly List<TrainingSlot> trainingSlots = new List<TrainingSlot>();
    private float lastGameTime;

    protected abstract TrainingConfig[] GetUpgradeConfigs();
    protected abstract int GetMaxTraineeCapacity();

    public override void Awake()
    {
        base.Awake();
        maxTraineeCapacity = GetMaxTraineeCapacity();
        availableConfigs = GetUpgradeConfigs();
    }

    protected virtual void Start()
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

    protected virtual void ProcessTraining(float gameTimeDelta)
    {
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

    protected virtual void GraduateTrainee(TrainingSlot slot)
    {
        Unit unit = slot.npcUnit;
        unit.gameObject.SetActive(true);

        RemoveUnit(unit);
        UnitManager.Instance.allUnits.Remove(unit);
        PoolManager.Instance.Despawn(unit.gameObject);

        var spawnPrefab = slot.trainingConfig.unitPrefab;
        if (spawnPrefab != null)
        {
            var newUnit = PoolManager.Instance.Spawn(spawnPrefab, 
                GetRandomPositionAroundBuilding(), 
                Quaternion.identity);

            var unitComponent = newUnit.GetComponent<Unit>();
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

    public override void AddUnit(Unit unit)
    {
        if (unit.unitType == UnitType.Civilian)
        {
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

    public virtual void AddTraineeWithSelection(Unit unit, UnitType targetType)
    {
        if (!CanAddUnit(unit)) return;

        TrainingConfig selectedConfig = availableConfigs.FirstOrDefault(c => c.targetType == targetType);
        if (selectedConfig.unitPrefab == null) return;

        AddTrainee(unit, selectedConfig);
        SyncDebugView();
    }

    private void AddTrainee(Unit unit, TrainingConfig config)
    {
        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;

        trainingSlots.Add(new TrainingSlot(unit, config));
        currentTraineeCount = trainingSlots.Count;

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

    protected virtual void SyncDebugView()
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

    public TrainingConfig[] GetAvailableConfigs() => availableConfigs;
    
    public virtual List<TraineeSaveData> GetTraineesSaveData()
    {
        return trainingSlots.Select(slot => new TraineeSaveData
        {
            unitID = slot.npcUnit.GetId(),
            currentTrainingHours = slot.currentTrainingHours,
            targetType = slot.trainingConfig.targetType 
        }).ToList();
    }

    public virtual void ForceAddTraineeOnLoad(Unit unit, float savedHours, UnitType targetType)
    {
        TrainingConfig config = availableConfigs.FirstOrDefault(c => c.targetType == targetType);
        var newSlot = new TrainingSlot(unit, config);
        newSlot.currentTrainingHours = savedHours;
        
        trainingSlots.Add(newSlot);
        currentTraineeCount = trainingSlots.Count;

        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;
        unit.transform.position = transform.position;
        unit.gameObject.SetActive(false);

        SyncDebugView();
    }
}