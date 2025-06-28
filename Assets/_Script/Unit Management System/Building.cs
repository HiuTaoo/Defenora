using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Building : MonoBehaviour
{
    [Header("Building Info")]
    public string buildingName;
    public int maxCapacity = 5;
    public float range = 5f;  // Tầm phát hiện kẻ địch hoặc bán kính và kị sĩ có thể đi tuần tra 

    [Header("Unit Management")]
    public List<Unit> stationedUnits = new List<Unit>();
    public Transform[] unitPositions; // Vị trí đặt nhân vật

    [Header("Tầng mà công trình được đặt")]
    public int layerIndex = 0;

    // Thêm nhân vật vào trạm
    public virtual bool AddUnit(Unit unit)
    {
        if (stationedUnits.Count >= maxCapacity)
        {
            Debug.Log($"Trạm {buildingName} đã đầy!");
            return false;
        }

        if (!stationedUnits.Contains(unit))
        {
            stationedUnits.Add(unit);

            // Đặt nhân vật tại vị trí trống
            Transform availableSpot = GetAvailableSpot();
            if (availableSpot != null)
            {
                unit.transform.position = availableSpot.position;
            }

            unit.currentState = UnitState.Stationed;
            Debug.Log($"{unit.unitName} đã được đặt tại {buildingName}");
            return true;
        }

        return false;
    }

    // Loại bỏ nhân vật khỏi trạm
    public virtual bool RemoveUnit(Unit unit)
    {
        if (stationedUnits.Contains(unit))
        {
            stationedUnits.Remove(unit);
            unit.currentState = UnitState.Idle;
            Debug.Log($"{unit.unitName} đã rời khỏi {buildingName}");
            return true;
        }

        return false;
    }

    
    // Lấy vị trí trống
    public virtual Transform GetAvailableSpot()
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

    public BuildingInfo GetStationInfo()
    {
        return new BuildingInfo
        {
            stationName = this.buildingName,
            currentCapacity = stationedUnits.Count,
            maxCapacity = this.maxCapacity,
            unitNames = stationedUnits.Select(u => u.unitName).ToList()
        };
    }

/*    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Building building = GetComponent<Building>();
            Unit unit = other.GetComponent<Unit>();
            if (building != null && unit != null)
            {
                building.AddUnit(unit);
                Debug.Log("Player đã vào vùng của công trình.");
            }
        }
    }*/


}
