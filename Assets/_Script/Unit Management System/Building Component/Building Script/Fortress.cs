using _Script.Unit_Management_System.Building;
using UnityEngine;

public class Fortress : Building
{
    private GuardComponent guardComponent;

    private void Awake()
    {
        base.Awake();
        buildingType = BuildingType.Fortress;
        guardComponent = gameObject.GetComponent<GuardComponent>();
    }

    protected override void OnUnitAdded(Unit unit)
    {
        GetComponent<GuardComponent>()?.OnUnitAdded(unit);
    }

    protected override void OnUnitRemoved(Unit unit)
    {
        GetComponent<GuardComponent>()?.OnUnitRemoved(unit);

        Vector3 availableSpot = GetRandomPositionAroundBuilding();

        if (availableSpot != null) 
        {
            unit.transform.position = availableSpot;
        }
    }
    
    public override void AddUnit(Unit unit)
    {
        base.AddUnit(unit);

        if (unit is Archer archer) archer.isStationed = true;
    }

    public override bool RemoveUnit(Unit unit)
    {
        var removed = base.RemoveUnit(unit);

        if (removed && unit is Archer archer) archer.isStationed = false;

        return removed;
    }

    public override void ForceAddUnitOnLoad(Unit unit)
    {
        base.ForceAddUnitOnLoad(unit);

        if (unit is Archer archer) archer.isStationed = true;
    }
}