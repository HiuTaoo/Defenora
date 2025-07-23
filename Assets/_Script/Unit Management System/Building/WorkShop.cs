using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkShop : Building
{
    private void Awake()
    {
        base.Awake();
        buildingType = BuildingType.WorkShop;
    }

    /*public override void AddUnit(Unit unit)
    {
        if (unit.unitType != UnitType.Builder)
            return;

        if (currentCapacity >= maxCapacity)
        {
            Debug.Log($"Trạm {buildingName} đã đầy!");
            return;
        }

        if (!stationedUnits.Contains(unit))
        {
            stationedUnits.Add(unit);
            unit.floorAgent.MoveToFloor(LayerIndex);
            unit.assignedBuilding = this;
            currentCapacity++;

            Debug.Log($"Register {unit.name} to Building: {this.name}");

            Vector3 availableSpot = GetRandomPositionAroundBuilding();
            if (availableSpot != null)
            {
                unit.transform.position = availableSpot;
            }

            unit.currentState = UnitState.Stationed;
            unit.floorAgent.MoveToFloor(LayerIndex);
            return true;
        }
        return false;
    }*/
}
