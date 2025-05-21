using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine;
using System.Collections.Generic;

public class MenuTilesController : MonoBehaviour
{
    public static MenuTilesController Instance;

    [Header("Tilemap Layers")]
    [SerializeField] public List<TileLayerEntry> tileLayers;

    [Header("List Tile")]
    [SerializeField] private TileBase[] tiles;

    [Header("mouseIndicator")]
    public GameObject mouseIndicator;

    [Header("Camera")]
    [SerializeField] private Camera sceneCamera;

    [Header("Tile Rules")]
    public List<TilePlacementRule> tileRules;

    [Header("Variable")]
    public Tilemap targetTilemap;
    public TileBase selectedTile;
    public int selectedLayerIndex = 0;
    public TileBase emptyTile;

    private bool isDragging = false;
    public bool editMode = true;
    public bool isDeleteTile = false;
    public bool canPlaceTile = false;

    private HashSet<Vector3Int> positionSet = new HashSet<Vector3Int>();

    private Vector3Int lastPlacedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    private Vector3Int lastErasedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        HandleMouseInput();
        CheckCanPlaceTile();
    }

    private void HandleMouseInput()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (targetTilemap == null) return;

        Vector3 mouseWorldPos = sceneCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = targetTilemap.WorldToCell(mouseWorldPos);

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            if (isDeleteTile)
                EraseTile(cellPosition);
            else
            {
                TryPlaceTile(cellPosition);
                positionSet.Add(cellPosition);
            }
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            if (isDeleteTile)
                EraseTile(cellPosition);
            else
            {
                TryPlaceTile(cellPosition);
                positionSet.Add(cellPosition);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            foreach (var cell in positionSet)
            {
                PlaceLinkTile(cell);
            }
            positionSet.Clear();
        }
    }

    private void TryPlaceTile(Vector3Int cellPosition)
    {
        if (selectedTile == null || cellPosition == lastPlacedCell)
            return;

        TileBase currentTile = targetTilemap.GetTile(cellPosition);
        if (currentTile != null && currentTile == selectedTile)
            return;

        if (CanPlaceOnThisLayer(cellPosition))
        {
            PlaceTile(cellPosition);
        }
    }

    private void PlaceTile(Vector3Int cellPosition)
    {
        targetTilemap.SetTile(cellPosition, selectedTile);
        lastPlacedCell = cellPosition;
    }

    public void PlaceLinkTile(Vector3Int cellPosition)
    {
        var link = TileLinkManager.Instance.GetLink(selectedTile, targetTilemap);
        if (link != null && link.linkedTile != null && link.linkedTilemap != null)
        {
            link.linkedTilemap.SetTile(cellPosition, link.linkedTile);
        }
    }

    private void EraseTile(Vector3Int cellPosition)
    {
        if (cellPosition == lastErasedCell)
            return;

        // Tìm index của targetTilemap trong danh sách
        int startIndex = tileLayers.FindIndex(e => e.tilemap == targetTilemap);
        if (startIndex == -1)
        {
            Debug.LogWarning("Target Tilemap không nằm trong danh sách tileLayers.");
            return;
        }

        // Xóa từ tầng hiện tại trở lên
        for (int i = startIndex; i < tileLayers.Count; i++)
        {
            Tilemap map = tileLayers[i].tilemap;
            if (map.GetTile(cellPosition) != null)
            {
                map.SetTile(cellPosition, null);
            }
        }

        lastErasedCell = cellPosition;

        // Xóa tile liên kết nếu có
        TileBase tile = targetTilemap.GetTile(cellPosition);
        var link = TileLinkManager.Instance.GetLink(tile, targetTilemap);
        if (link != null && link.linkedTilemap != null)
        {
            link.linkedTilemap.SetTile(cellPosition, null);
        }
    }

    public void ToggleDeleteTileMode()
    {
        isDeleteTile = !isDeleteTile;
        lastPlacedCell = Vector3Int.zero;
        lastErasedCell = Vector3Int.zero;
    }

    public void TogglePlaceTileMode()
    {
        isDeleteTile = false;
        lastPlacedCell = Vector3Int.zero;
        lastErasedCell = Vector3Int.zero;
    }

    public void CheckCanPlaceTile()
    {
        canPlaceTile = TilemapHover.Instance.canPlace;
    }

    public bool CanPlaceOnThisLayer(Vector3Int cellPos)
    {
        if (selectedTile == null || targetTilemap == null)
            return false;

        var rule = GetRuleForTile(selectedTile);
        if (rule == null || rule.requiredBaseTilemapNames.Count == 0)
            return true;

        foreach (var layer in tileLayers)
        {
            if (rule.requiredBaseTilemapNames.Contains(layer.tilemap.name))
            {
                if (layer.tilemap.GetTile(cellPos) != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private TilePlacementRule GetRuleForTile(TileBase tile)
    {
        foreach (var rule in tileRules)
        {
            if (rule.tile == tile)
                return rule;
        }
        return null;
    }

    public void UpdateTilemapLayer()
    {
        foreach (var layer in tileLayers)
        {
            if(selectedTile != null)
            {
                if (layer.layerIndex == selectedLayerIndex && layer.tileType == selectedTile.name)
                {
                    targetTilemap = layer.tilemap;
                    return;
                }
            }
        }
    }
}
