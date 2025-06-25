using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Station : MonoBehaviour
{
    [Header("Station Info")]
    public string stationName;
    public StationType stationType;
    public int maxCapacity = 5;

    [Header("Unit Management")]
    public List<Unit> stationedUnits = new List<Unit>();
    public Transform[] unitPositions; // Vị trí đặt nhân vật

    public enum StationType
    {
        Watchtower, // Tháp canh
        Fortress    // Thành trì
    }

    // Thêm nhân vật vào trạm
    public bool AddUnit(Unit unit)
    {
        if (stationedUnits.Count >= maxCapacity)
        {
            Debug.Log($"Trạm {stationName} đã đầy!");
            return false;
        }

        if (!stationedUnits.Contains(unit))
        {
            stationedUnits.Add(unit);

            // Đặt nhân vật tại vị trí trống
            Transform availablePosition = GetAvailablePosition();
            if (availablePosition != null)
            {
                unit.MoveTo(availablePosition);
            }

            unit.currentState = UnitState.Stationed;
            Debug.Log($"{unit.unitName} đã được đặt tại {stationName}");
            return true;
        }

        return false;
    }

    // Loại bỏ nhân vật khỏi trạm
    public bool RemoveUnit(Unit unit)
    {
        if (stationedUnits.Contains(unit))
        {
            stationedUnits.Remove(unit);
            unit.currentState = UnitState.Idle;
            Debug.Log($"{unit.unitName} đã rời khỏi {stationName}");
            return true;
        }

        return false;
    }

    // Lấy vị trí trống
    private Transform GetAvailablePosition()
    {
        if (unitPositions == null || unitPositions.Length == 0)
            return transform;

        for (int i = 0; i < unitPositions.Length && i < maxCapacity; i++)
        {
            bool positionOccupied = false;

            foreach (Unit unit in stationedUnits)
            {
                if (Vector3.Distance(unit.transform.position, unitPositions[i].position) < 0.5f)
                {
                    positionOccupied = true;
                    break;
                }
            }

            if (!positionOccupied)
                return unitPositions[i];
        }

        return transform;
    }

    // Lấy thông tin trạm
    public StationInfo GetStationInfo()
    {
        return new StationInfo
        {
            stationName = this.stationName,
            stationType = this.stationType,
            currentCapacity = stationedUnits.Count,
            maxCapacity = this.maxCapacity,
            unitNames = stationedUnits.Select(u => u.unitName).ToList()
        };
    }
}
