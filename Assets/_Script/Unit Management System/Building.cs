using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Building : MonoBehaviour
{
    [Header("Building Info")]
    public string buildingName;
    public int maxCapacity = 5;
    public float range = 5f;
    public int currentCapacity = 0;
    public BuildingType buildingType;

    [Header("Unit Management")]
    public List<Unit> stationedUnits = new List<Unit>();
    [Tooltip("Danh sách những điểm mà cung thủ có thể đứng")]
    public Transform[] positionSpots;
    [Tooltip("Lưu tên unit và vị trí đang đứng nếu đó là tháp canh ")]
    public List<SpotData> listArcherPositions = new List<SpotData>();
    [Header("Tầng mà công trình được đặt")]
    private int layerIndex = 0;

    private SpriteRenderer spriteRenderer;


    public int LayerIndex
    {
        get => layerIndex;
        set
        {
            layerIndex = value;

        }
    }

    public void RegisterSpot()
    {
        List<Transform> spots = new List<Transform>();

        // Quét tất cả con trong Hierarchy
        foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("Spot"))
            {
                spots.Add(child);
            }
        }

        positionSpots = spots.ToArray();

    }

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

            unit.assignedBuilding = this;
            if(unit.unitType == UnitType.Archer)
            {
                Vector3 spot = GetAvailableSpot();
                if( spot != null )
                {
                    unit.transform.position = spot;
                    unit.spriteRenderer.sortingOrder = (100 * unit.floorAgent.currentFloorIndex) + 10;
                    int index = listArcherPositions.FindIndex(s => s.position == spot);
                    if (index >= 0)
                    {
                        var updated = listArcherPositions[index];
                        updated.unitName = unit.unitName;
                        listArcherPositions[index] = updated;
                    }
                    else
                    {
                        listArcherPositions.Add(new SpotData
                        {
                            position = spot,
                            unitName = unit.unitName
                        });
                    }


                }
            }
            else
            {
                Vector3 availableSpot = GetRandomPositionAroundBuilding();
                if (availableSpot != null)
                {
                    unit.transform.position = availableSpot;
                }
            }
            unit.currentState = UnitState.Stationed;
            return true;
        }

        return false;
    }

    public virtual bool RemoveUnit(Unit unit)
    {
        if (stationedUnits.Contains(unit))
        {
            stationedUnits.Remove(unit);
            unit.currentState = UnitState.Idle;
            unit.assignedBuilding = null;

            if (unit.unitType == UnitType.Archer)
            {
                int index = listArcherPositions.FindIndex(s => s.unitName == unit.unitName);
                if (index >= 0)
                {
                    var updated = listArcherPositions[index];
                    updated.unitName = null; // Đánh dấu slot trống
                    listArcherPositions[index] = updated;
                    Debug.Log($"Đã xóa vị trí cung thủ {unit.unitName} khỏi spot {updated.position}");
                }
            }

            Debug.Log($"{unit.unitName} đã rời khỏi {buildingName}");
            return true;
        }

        return false;
    }

    public virtual Vector3 GetAvailableSpot()
    {
        foreach (var spot in positionSpots)
        {
            SpotData? spotData = listArcherPositions
                .FirstOrDefault(s => s.position == spot.position);

            if (!spotData.HasValue || string.IsNullOrEmpty(spotData.Value.unitName))
            {
                return spot.position;
            }
        }

        // Hết chỗ: fallback random
        return GetRandomPositionAroundBuilding();
    }


    /// <summary>
    /// Trả về vị trí ngẫu nhiên trong phạm vi `range` quanh building.
    /// </summary>
    public Vector3 GetRandomPositionAroundBuilding()
    {
        Vector2 randomPoint = Random.insideUnitCircle * range;
        Vector3 basePosition = transform.position;

        return new Vector3(
            basePosition.x + randomPoint.x,
            basePosition.y, // hoặc giữ nguyên y
            basePosition.z + randomPoint.y
        );
    }

    public void UpdateRenderSortingOrder(int layerIndex)
    {
        var sr = GetSpriteRenderer();
        if (sr != null)
            sr.sortingOrder = (100 * layerIndex) + 2;
    }

    private SpriteRenderer GetSpriteRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        return spriteRenderer;
    }

    public BuildingData GetStationInfo()
    {
        return new BuildingData
        {
            buildingName = this.buildingName,
            currentCapacity = stationedUnits.Count,
            maxCapacity = this.maxCapacity,
            unitNames = stationedUnits.Select(u => u.unitName).ToList()
        };
    }

    

}
