using _Script.Unit_Management_System.Building;
using UnityEngine;

public class Barrack : TrainingBuilding
{
    protected override TrainingConfig[] GetUpgradeConfigs() => (configData as BarrackData)?.upgradeConfigs;
    protected override int GetMaxTraineeCapacity() => (configData as BarrackData)?.maxTraineeCapacity ?? 3;
    
    private GuardComponent guardComponent;

    public override void Awake()
    {
        base.Awake();
        guardComponent = GetComponent<GuardComponent>();
    }
    
    #region Phân Luồng Logic Gác Đêm (Guard System)
    protected override void OnUnitAdded(Unit unit)
    {
        if (unit.unitType == UnitType.Civilian)
        {
            return; 
        }

        if (guardComponent != null)
        {
            guardComponent.OnUnitAdded(unit);
        }
    }

    protected override void OnUnitRemoved(Unit unit)
    {
        if (unit.unitType == UnitType.Civilian) return;

        if (guardComponent != null)
        {
            guardComponent.OnUnitRemoved(unit);
        }

        Vector3 availableSpot = GetRandomPositionAroundBuilding();
        if (availableSpot != null) 
        {
            unit.transform.position = availableSpot;
        }
    }

    public override void AddUnit(Unit unit)
{
    // -----------------------------------------------------------------
    // TRƯỜNG HỢP 1: CÔNG DÂN (CIVILIAN) - ĐƯA VÀO HÀNG CHỜ HUẤN LUYỆN
    // -----------------------------------------------------------------
    if (unit.unitType == UnitType.Civilian)
    {
        if (trainingSlots.Count >= maxTraineeCapacity) return;

        TrainingConfig defaultConfig = (availableConfigs != null && availableConfigs.Length > 0) 
            ? availableConfigs[0] 
            : default;

        ConsumeResources(defaultConfig);

        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;

        trainingSlots.Add(new TrainingSlot(unit, defaultConfig));
        currentTraineeCount = trainingSlots.Count;

        unit.transform.position = transform.position;
        unit.gameObject.SetActive(false);

        SyncDebugView();
        return; 
    }

    // -----------------------------------------------------------------
    // LOGIC TRÚ ĐÓNG CHUNG CHO ĐƠN VỊ CHIẾN ĐẤU / ĐƠN VỊ KHÁC
    // -----------------------------------------------------------------
    if (currentCapacity >= maxCapacity) return;

    stationedUnits.Add(unit);
    unit.characterMovement.CurrentLayer = LayerIndex;
    unit.floorAgent.MoveToFloor(LayerIndex);
    unit.assignedBuilding = this;
    currentCapacity++;

    // -----------------------------------------------------------------
    // TRƯỜNG HỢP 2: CUNG THỦ (ARCHER) - CHECK ĐẦY Ô GÁC CHUẨN XÁC
    // -----------------------------------------------------------------
    if (unit is Archer archer)
    {
        if (guardComponent != null)
        {
            if (guardComponent.listArcherPositions.Count >= guardComponent.positionSpots.Length)
            {
                unit.transform.position = GetRandomPositionAroundBuilding();
                AudioManager.Instance.PlaySFX(SoundNames.SfxSuccess);
                Debug.Log($"[Barrack] Tháp canh đã đầy chỗ ({guardComponent.listArcherPositions.Count}/{guardComponent.positionSpots.Length})! Xếp {unit.unitName} đứng dưới đất.");
            }
            else
            {
                Vector3 targetSpot = guardComponent.GetAvailableSpot();
                unit.transform.position = targetSpot;
                
                guardComponent.listArcherPositions.Add(new SpotData
                {
                    position = targetSpot,
                    unitId = unit.GetId()
                });
                archer.isStationed = true;
                AudioManager.Instance.PlaySFX(SoundNames.SfxSuccess);
                Debug.Log($"[Barrack] Tháp còn trống! Đã đưa {unit.unitName} lên vị trí gác: {targetSpot}");
            }
        }
    }
    // -----------------------------------------------------------------
    // TRƯỜNG HỢP 3: CÁC LOẠI UNIT KHÁC (BUILDER, WARRIOR, KNIGHT...)
    // -----------------------------------------------------------------
    else
    {
        unit.transform.position = GetRandomPositionAroundBuilding();
        AudioManager.Instance.PlaySFX(SoundNames.SfxSuccess);
    }

    OnStationedUnitsChanged?.Invoke();
    SyncDebugView();
}

    public override bool RemoveUnit(Unit unit)
    {
        var removed = base.RemoveUnit(unit);

        if (removed && unit is Archer archer) 
        {
            archer.isStationed = false;
        }

        return removed;
    }

    public override void ForceAddUnitOnLoad(Unit unit)
    {
        base.ForceAddUnitOnLoad(unit);

        if (unit is Archer archer) 
        {
            archer.isStationed = true;
        }
    }

    #endregion
    
    private void ConsumeResources(TrainingConfig config)
    {
        if (config.trainingCosts == null || config.trainingCosts.Length == 0) return;
        if (Inventory.Instance == null) return;

        foreach (var cost in config.trainingCosts)
        {
            if (cost.itemData == null || cost.amount <= 0) continue;
            
            Inventory.Instance.Remove(cost.itemData, cost.amount);
            Debug.Log($"[Training] Đã khấu trừ {cost.amount}x {cost.itemData.name} cho việc huấn luyện.");
        }
    }
}