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

    [Tooltip("Tầng mà công trình được đặt")]
    private int layerIndex = 0;

    private SpriteRenderer spriteRenderer;
    private BuildingFootprint buildingFootprint;

    public int LayerIndex
    {
        get => layerIndex;
        set
        {
            layerIndex = value;
        }
    }

    private void Awake()
    {
        buildingFootprint = GetComponent<BuildingFootprint>();
    }

    public void RegisterSpot()
    {
        List<Transform> spots = new List<Transform>();

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
        if (currentCapacity >= maxCapacity)
        {
            Debug.Log($"Trạm {buildingName} đã đầy!");
            return false;
        }

        if (!stationedUnits.Contains(unit))
        {
            stationedUnits.Add(unit);
            unit.floorAgent.MoveToFloor(LayerIndex);
            unit.assignedBuilding = this;
            currentCapacity++;

            Debug.Log($"Register {unit.name} to Building: {this.name}");

            if (unit.unitType == UnitType.Archer)
            {
                Vector3 spot = GetAvailableSpot();
                if (spot != null)
                {
                    unit.transform.position = spot;
                    unit.spriteRenderer.sortingOrder = spriteRenderer.sortingOrder + 10;

                    listArcherPositions.Add(new SpotData
                    {
                        position = spot,
                        unitName = unit.unitName
                    });
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
            unit.floorAgent.MoveToFloor(LayerIndex);
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
            currentCapacity--;

            if (unit.unitType == UnitType.Archer)
            {
                int index = listArcherPositions.FindIndex(s => s.unitName == unit.unitName);
                if (index >= 0)
                {
                    var removed = listArcherPositions[index];
                    listArcherPositions.RemoveAt(index);
                }
            }
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

        return GetRandomPositionAroundBuilding();
    }

    public Vector3 GetRandomPositionAroundBuilding()
    {
        const int maxTries = 20;
        float cellSize = 1f; 

        Vector3 basePosition = transform.position;
        GraphNode graphNode = GraphNode.Instance;

        for (int i = 0; i < maxTries; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * range;
            Vector3 randomWorldPos = new Vector3(
                basePosition.x + randomOffset.x,
                basePosition.y + randomOffset.y,
                0f 
            );

            Vector2Int cellPos = WorldToCell(randomWorldPos, cellSize);

            Node node = graphNode.GetNode(new Vector3Int(cellPos.x, cellPos.y, 0), LayerIndex);

            if (node != null && node.isWalkable)
            {
                Vector3 cellCenter = CellToWorld(cellPos, cellSize);
                return cellCenter;
            }
        }

        Debug.LogWarning($"Không tìm được Node walkable quanh Building. Trả về vị trí Building.");
        Vector2Int fallbackCell = WorldToCell(basePosition, cellSize);
        return CellToWorld(fallbackCell, cellSize);
    }

    private Vector2Int WorldToCell(Vector3 worldPos, float cellSize)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    private Vector3 CellToWorld(Vector2Int cellPos, float cellSize)
    {
        return new Vector3(
            (cellPos.x + 0.5f) * cellSize,
            (cellPos.y + 0.5f) * cellSize,
            0f
        );
    }


    /*/// <summary>
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
    }*/

    public void UpdateRenderSortingOrder(int layerIndex)
    {
        GetSpriteRenderer();
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = (100 * layerIndex) + 5;
    }

    private void GetSpriteRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
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
