using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Khai báo footprint của công trình: tập hợp các cell chiếm chỗ tính từ Anchor.
/// Thường gắn trực tiếp lên prefab.
/// </summary>
[DisallowMultipleComponent]
public class BuildingFootprint : MonoBehaviour
{
    /// <summary>
    /// Các cell mà công trình chiếm chỗ, tính từ anchor point.
    /// (0,0) thường là ô gốc dưới cùng trung tâm.
    /// </summary>
    [Tooltip("Các cell mà công trình chiếm chỗ, tính từ anchor.")]
    public List<Vector2Int> occupiedCells = new List<Vector2Int>() { Vector2Int.zero };

    /// <summary>
    /// Trả về vị trí world của tất cả cell chiếm chỗ, dựa vào anchor world position.
    /// </summary>
    public List<Vector3> GetWorldPositions(Vector3 anchorWorldPos, float cellSize)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var offset in occupiedCells)
        {
            Vector3 cellPos = anchorWorldPos + new Vector3(offset.x * cellSize, offset.y * cellSize, 0f);
            positions.Add(cellPos);
        }
        return positions;
    }

    /// <summary>
    /// Trả về vị trí cell grid tuyệt đối.
    /// anchorGridPos là cell gốc.
    /// </summary>
    public List<Vector2Int> GetAbsoluteGridPositions(Vector2Int anchorGridPos)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        foreach (var offset in occupiedCells)
        {
            positions.Add(anchorGridPos + offset);
        }
        return positions;
    }

    /// <summary>
    /// Debug: Vẽ Gizmos footprint trong Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        // Giả sử mỗi ô = 1 unit
        float cellSize = 1f;

        Vector3 anchor = transform.position;

        foreach (var offset in occupiedCells)
        {
            Vector3 cellPos = anchor + new Vector3(offset.x * cellSize, offset.y * cellSize, 0f);
            Gizmos.DrawWireCube(cellPos, Vector3.one * cellSize);
        }

        // Vẽ ô anchor nổi bật
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(anchor, Vector3.one * cellSize * 1.1f);
    }
}
