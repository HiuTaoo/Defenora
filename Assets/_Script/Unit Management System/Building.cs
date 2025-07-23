using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public abstract class Building : MonoBehaviour
{
    [Header("Building Info")]
    public string buildingName;
    public int maxCapacity = 5;
    public float range = 5f;
    public float maxHealth = 10f;
    public float currentHealth = 10f; 
    public int currentCapacity = 0;
    public BuildingType buildingType;
    public BuildingState buildingState;

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

    public void Awake()
    {
        buildingFootprint = GetComponent<BuildingFootprint>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        buildingName = gameObject.name;
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

    #region Unit Management
    public bool CanAddUnit(Unit unit)
    {
        if (currentCapacity >= maxCapacity)
        {
            Debug.Log($"Trạm {buildingName} đã đầy!");
            return false;
        }
        if (unit.unitType == UnitType.Builder && buildingType != BuildingType.WorkShop)
            return false;

        if (unit.unitType != UnitType.Builder && buildingType == BuildingType.WorkShop)
            return false;

        return !stationedUnits.Contains(unit);
    }

    public virtual void AddUnit(Unit unit)
    {
        stationedUnits.Add(unit);
        unit.floorAgent.MoveToFloor(LayerIndex);
        unit.assignedBuilding = this;
        currentCapacity++;

        Debug.Log($"Register {unit.name} to Building: {this.name}");

        if (GameLoop.Instance.StateMachine.CurrentState is EditorState)
            RegisterUnitPosition(unit);

        unit.currentState = UnitState.Stationed;
        //unit.floorAgent.MoveToFloor(LayerIndex);
    }

    private void RegisterUnitPosition(Unit unit)
    {
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
                    unitName = unit.gameObject.name
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

    
    #endregion

    #region RANDOM POSITION
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
        const int maxTries = 10; 
        float cellSize = 1f;

        if (buildingFootprint == null)
            GetBuildingFootprinf();

        var cells = buildingFootprint.GetAbsoluteGridPositions(WorldToCell(transform.position, 1f));
        Vector3 basePosition = transform.position;
        GraphNode graphNode = GraphNode.Instance;

        List<Vector3> validPositions = new List<Vector3>();

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomWorldPos = GetRandomPositionInRange(basePosition);
            Vector2Int cellPos = WorldToCell(randomWorldPos, cellSize);

            bool isInFootprint = false;
            foreach (var cell in cells)
            {
                if (cellPos == cell)
                {
                    isInFootprint = true;
                    break;
                }
            }

            if (isInFootprint) continue;

            Node node = graphNode.GetNode(new Vector3Int(cellPos.x, cellPos.y, 0), LayerIndex);
            if (node != null && node.isWalkable)
            {
                Vector3 cellCenter = CellToWorld(cellPos, cellSize);
                validPositions.Add(cellCenter);
            }
        }

        if (validPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, validPositions.Count);
            return validPositions[randomIndex];
        }

        Debug.LogWarning($"Không tìm được Node walkable quanh Building. Trả về vị trí Building.");
        Vector2Int fallbackCell = WorldToCell(basePosition, cellSize);
        return CellToWorld(fallbackCell, cellSize);
    }

    private Vector3 GetRandomPositionInRange_UniformCircle(Vector3 basePosition)
    {
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float radius = Mathf.Sqrt(Random.Range(0f, 1f)) * range / 2;

        Vector2 randomOffset = new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );

        return new Vector3(
            basePosition.x + randomOffset.x,
            basePosition.y + randomOffset.y,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange_SquareToCircle(Vector3 basePosition)
    {
        Vector2 randomOffset;
        do
        {
            randomOffset = new Vector2(
                Random.Range(-range / 2, range / 2),
                Random.Range(-range / 2, range / 2)
            );
        } while (randomOffset.magnitude > range / 2);

        return new Vector3(
            basePosition.x + randomOffset.x,
            basePosition.y + randomOffset.y,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange_GridPattern(Vector3 basePosition)
    {
        int gridSize = Mathf.RoundToInt(range);
        int randomX = Random.Range(-gridSize / 2, gridSize / 2 + 1);
        int randomY = Random.Range(-gridSize / 2, gridSize / 2 + 1);

        float noiseX = Random.Range(-0.3f, 0.3f);
        float noiseY = Random.Range(-0.3f, 0.3f);

        return new Vector3(
            basePosition.x + randomX + noiseX,
            basePosition.y + randomY + noiseY,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange_WeightedDistance(Vector3 basePosition)
    {
        float minDistance = 2f; 
        float maxDistance = range / 2;

        float angle = Random.Range(0f, 2f * Mathf.PI);
        float radius = Random.Range(minDistance, maxDistance);

        Vector2 randomOffset = new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );

        return new Vector3(
            basePosition.x + randomOffset.x,
            basePosition.y + randomOffset.y,
            0f
        );
    }

    private Vector3 GetRandomPositionInRange(Vector3 basePosition)
    {
        int method = Random.Range(0, 4);

        switch (method)
        {
            case 0: return GetRandomPositionInRange_UniformCircle(basePosition);
            case 1: return GetRandomPositionInRange_SquareToCircle(basePosition);
            case 2: return GetRandomPositionInRange_GridPattern(basePosition);
            case 3: return GetRandomPositionInRange_WeightedDistance(basePosition);
            default: return GetRandomPositionInRange_UniformCircle(basePosition);
        }
    }

    #endregion

    #region Helper Methods
    public Vector2Int WorldToCell(Vector3 worldPos, float cellSize)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorld(Vector2Int cellPos, float cellSize)
    {
        return new Vector3(
            (cellPos.x + 0.5f) * cellSize,
            (cellPos.y + 0.5f) * cellSize,
            0f
        );
    }

    public void UpdateRenderSortingOrder(int layerIndex)
    {
        GetSpriteRenderer();
        var renderData = RenderManager.Instance.LookUpRenderDataByLayerIndex(RenderManager.Instance.decorRender, layerIndex);
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = renderData.sortingOrder;
    }

    private void GetSpriteRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void GetBuildingFootprinf()
    {
        if(buildingFootprint == null)
            buildingFootprint = GetComponent<BuildingFootprint>();
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

    public SpriteRenderer GetSpriteRendererComponent()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        return spriteRenderer;
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (positionSpots == null) return;

        Gizmos.color = Color.green;
        foreach (Transform spot in positionSpots)
        {
            if (spot != null)
                Gizmos.DrawSphere(spot.position, 0.1f);
        }
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif


}
