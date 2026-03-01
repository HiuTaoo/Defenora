using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Unit_Management_System.Building;
using UnityEngine;

public class Watchtower : Building
{
    private GuardComponent guardComponent;
    private void Awake()
    {
        base.Awake();
        buildingType = BuildingType.WatchTower;
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
}
