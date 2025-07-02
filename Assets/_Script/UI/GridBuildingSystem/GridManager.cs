using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý grid, cell đã chiếm.
/// </summary>
public class GridManager : MonoBehaviour
{
    public HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    public List<Transform> listBuilding = new List<Transform>();

    [SerializeField] private float cellSize = 1f;

    private GraphNode graphNode;

    private void Awake()
    {
        graphNode = FindObjectOfType<GraphNode>();
        if (graphNode == null)
            Debug.LogError("GraphNode không tồn tại trên scene!");

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

        listBuilding.Add(footprint.transform);
        // Spawn building thật (bạn tự tuỳ biến).
        GameObject currentbuilding =  Instantiate(footprint.gameObject, CellToWorld(anchorCell), Quaternion.identity);
        currentbuilding.transform.SetParent(this.gameObject.transform);
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

    /*public bool CanPlaceFootprint(Vector2Int anchorCell, BuildingFootprint footprint, int layerIndex)
    {
        var cells = footprint.GetAbsoluteGridPositions(anchorCell);

        foreach (var cell in cells)
        {
            if (occupiedCells.Contains(cell))
            {
                return false;
            }

            Node node = graphNode.GetNode(new Vector3Int(cell.x, cell.y, 0), layerIndex);
            if (node == null || !node.isWalkable)
            {
                return false;
            }
        }

        return true;
    }*/
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

            // ➜ Anchor & các ô không nằm ở mép
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

            // Luôn kiểm tra node walkable cho mọi ô
            Node node = graphNode.GetNode(new Vector3Int(cell.x, cell.y, 0), layerIndex);
            if (node == null || !node.isWalkable)
                return false;

            // Đặc biệt: anchor thì không được occupied
            if (cell == anchorCell && occupiedCells.Contains(cell))
                return false;
        }

        return true;
    }





}
