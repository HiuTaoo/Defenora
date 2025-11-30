using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BridgeTilemapManager : MonoBehaviour
{
    public static BridgeTilemapManager Instance { get; private set; }

    [Header("Danh sách tilemap cầu và tầng của chúng")]
    [SerializeField]
    private List<BridgeTilemapEntry> bridgeMaps = new List<BridgeTilemapEntry>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (bridgeMaps.Count == 0)
            Debug.LogWarning("Chưa cấu hình Tilemap cầu trong Inspector.");
    }

    /// <summary>
    /// Kiểm tra worldPos có nằm trên cầu cùng layerIndex hay không.
    /// Nếu đúng -> trả sortingOrder của tilemap cầu đó.
    /// </summary>
    public bool TryGetBridgeSortingOrder(Vector3 worldPos, int characterLayerIndex, out int sortingOrder)
    {
        sortingOrder = 0;
        bool found = false;

        foreach (var entry in bridgeMaps)
        {
            // Chỉ xét tilemap thuộc cùng tầng
            if (entry.layerIndex != characterLayerIndex)
                continue;

            var tm = entry.tilemap;
            if (tm == null)
                continue;

            Vector3Int cell = tm.WorldToCell(worldPos);
            if (!tm.HasTile(cell))
                continue;

            var renderer = tm.GetComponent<TilemapRenderer>();
            if (renderer == null)
                continue;

            int order = renderer.sortingOrder;

            if (!found || order > sortingOrder)
            {
                sortingOrder = order;
                found = true;
            }
        }

        return found;
    }
}


[System.Serializable]
public struct BridgeTilemapEntry
{
    public Tilemap tilemap;
    public int layerIndex;
}