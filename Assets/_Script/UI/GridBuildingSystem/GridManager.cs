using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Rendering.ShadowCascadeGUI;

/// <summary>
/// Quản lý grid, cell đã chiếm.
/// </summary>
public class GridManager : MonoBehaviour
{
    public HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    public List<Building> listPlacedBuilding = new List<Building>();

    [SerializeField] private float cellSize = 1f;

    private GraphNode graphNode;
    private SaveLoadSystem saveLoadSystem;

    private void Awake()
    {
        graphNode = FindObjectOfType<GraphNode>();
        saveLoadSystem = FindObjectOfType<SaveLoadSystem>();

        if (graphNode == null)
            Debug.LogError("GraphNode không tồn tại trên scene!");

        if (saveLoadSystem != null)
            saveLoadSystem.OnSave += HandleSaveBuilding;


    }

    /// <summary>
    /// Kiểm tra ô đã chiếm.
    /// </summary>
    public bool IsCellOccupied(Vector2Int cellPos)
    {
        return occupiedCells.Contains(cellPos);
    }

    /// <summary>
    /// Đánh dấu ô là đã chiếm.
    /// </summary>
    public void PlaceBuilding(Vector2Int anchorCell, BuildingFootprint footprint)
    {
        var cells = footprint.GetAbsoluteGridPositions(anchorCell);
        foreach (var cell in cells)
        {
            occupiedCells.Add(cell);
        }

        // Spawn building thật (bạn tự tuỳ biến).
        GameObject currentbuilding =  Instantiate(footprint.gameObject, CellToWorld(anchorCell), Quaternion.identity);
        currentbuilding.transform.SetParent(this.gameObject.transform);

        Building building = currentbuilding.GetComponent<Building>();
        building.LayerIndex = BuildingGhostPreviewSystem.Instance.layerManager.layerIndex;

        currentbuilding.transform.name = $"{building.buildingType}: {System.Guid.NewGuid()}";
        listPlacedBuilding.Add(building);

    }

    private Vector3 CellToWorld(Vector2Int cellPos)
    {
        float cellSize = 1f; 
        float halfCell = cellSize * 0.5f;

        return new Vector3(
            cellPos.x * cellSize + halfCell,
            cellPos.y * cellSize + halfCell,
            0f
        );
    }

    public bool CanPlaceFootprint(Vector2Int anchorCell, BuildingFootprint footprint, int layerIndex)
    {
        var cells = footprint.GetAbsoluteGridPositions(anchorCell);

        int minX = int.MaxValue;
        int maxX = int.MinValue;

        foreach (var cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
        }

        foreach (var cell in cells)
        {
            bool isLeftEdge = (cell.x == minX);
            bool isRightEdge = (cell.x == maxX);

            if (!isLeftEdge && !isRightEdge)
            {
                if (occupiedCells.Contains(cell))
                    return false;
            }
            else
            {
                // ➜ Ô ngoài cùng thì vẫn kiểm tra node walkable, nhưng cho phép occupied
                // Không có gì phải làm ở đây
            }

            Node node = graphNode.GetNode(new Vector3Int(cell.x, cell.y, 0), layerIndex);
            if (node == null || !node.isWalkable || node.isStair)
                return false;

            if (cell == anchorCell && occupiedCells.Contains(cell))
                return false;
        }
        return true;
    }

    public void RollBackBuildingVisual()
    {
        foreach(var building in listPlacedBuilding) { 
            building.transform.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    private void HandleSaveBuilding() {
        UnitManager.Instance.buildings.AddRange(listPlacedBuilding);
        RollBackBuildingVisual();
        listPlacedBuilding.Clear();
    }
}
