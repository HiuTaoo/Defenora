using System.Collections;
using System.Collections.Generic;
using _Script.Data;
using _Script.Enum;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using _Script.Unit_Management_System.Building;
using UnityEngine;

public class Monastery : Building
{
    private class TrainingSlot
    {
        public Unit npcUnit;
        public float currentTrainingHours;

        public TrainingSlot(Unit unit)
        {
            npcUnit = unit;
            currentTrainingHours = 0f;
        }
    }

    [Header("Monastery Runtime Stats")]
    public float trainingDurationInGameHours = 4f;
    public int maxTraineeCapacity = 2;

    [Header("Debug View (Read Only)")]
    [SerializeField] private int currentTraineeCount = 0;
    [SerializeField] private List<TraineeDebugEntry> debugTrainees = new List<TraineeDebugEntry>();

    private readonly List<TrainingSlot> trainingSlots = new List<TrainingSlot>();
    private float lastGameTime;

    public override void Awake()
    {
        base.Awake(); 

        if (configData is MonasteryData monasteryConfig)
        {
            trainingDurationInGameHours = monasteryConfig.trainingDurationInGameHours;
            maxTraineeCapacity = monasteryConfig.maxTraineeCapacity;
        }
        else
        {
            Debug.LogError($"[Monastery] {gameObject.name} yêu cầu Config Data phải là loại MonasteryData!");
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

    private void GraduateTrainee(TrainingSlot slot)
    {
        Unit unit = slot.npcUnit;
    
        unit.gameObject.SetActive(true);

        RemoveUnit(unit);

        UnitManager.Instance.allUnits.Remove(unit);
    
        PoolManager.Instance.Despawn(unit.gameObject);
        
        var monk = PoolManager.Instance.Spawn(PrefabConfig.Instance.monkPrefab, 
            GetRandomPositionAroundBuilding(),
            Quaternion.identity);
            
        var monkComponent = monk.GetComponent<Monk>(); 
        UnitManager.Instance.RegisterUnit(monkComponent);
        
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

        if (currentCapacity >= maxCapacity || unit.unitType == UnitType.Builder)
            return false;

        return !stationedUnits.Contains(unit);
    }

    public override void AddUnit(Unit unit)
    {
        if (unit.unitType == UnitType.Civilian)
        {
            unit.floorAgent.MoveToFloor(LayerIndex);
            unit.assignedBuilding = this;
        
            trainingSlots.Add(new TrainingSlot(unit));
            currentTraineeCount = trainingSlots.Count;

            unit.transform.position = transform.position;
            unit.gameObject.SetActive(false);
        }
        else
        {
            base.AddUnit(unit); 
            Vector3 availableSpot = GetParentOrCreateSpotAroundBuilding();
            unit.transform.position = availableSpot;
        }

        SyncDebugView();
    }

    private Vector3 GetParentOrCreateSpotAroundBuilding()
    {
        return GetRandomPositionAroundBuilding();
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
    
    public List<TraineeSaveData> GetTraineesSaveData()
    {
        List<TraineeSaveData> list = new List<TraineeSaveData>();
        foreach (var slot in trainingSlots)
        {
            if (slot != null && slot.npcUnit != null)
            {
                list.Add(new TraineeSaveData
                {
                    unitID = slot.npcUnit.GetId(),
                    currentTrainingHours = slot.currentTrainingHours
                });
            }
        }
        return list;
    }
    
    public void ForceAddTraineeOnLoad(Unit unit, float savedHours)
    {
        var newSlot = new TrainingSlot(unit);
        newSlot.currentTrainingHours = savedHours;
        trainingSlots.Add(newSlot);
        currentTraineeCount = trainingSlots.Count;

        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;
        unit.transform.position = unit.assignedBuilding.transform.position;

        unit.gameObject.SetActive(false);

        SyncDebugView();
    }

    #region Debug Sync Logic
    private void SyncDebugView()
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

    #endregion
}