using _Script.Unit_Management_System.Building;

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
    }

    public override void ForceAddUnitOnLoad(Unit unit)
    {
        base.ForceAddUnitOnLoad(unit);

        if (unit is Archer archer) archer.isStationed = true;
    }
}