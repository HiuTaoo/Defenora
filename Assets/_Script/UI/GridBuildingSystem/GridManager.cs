using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý grid, cell đã chiếm.
/// </summary>
public class GridManager : MonoBehaviour
{
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

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
        Instantiate(footprint.gameObject, CellToWorld(anchorCell), Quaternion.identity);
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

}
