using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine;
using System.Collections.Generic;

public class MenuTilesController : MonoBehaviour
{
    public static MenuTilesController Instance;

    [Header("List Tile")]
    [SerializeField] private Tilemap[] tilemap;
    [SerializeField] private TileBase[] tiles;

    [Header("Variable")]
    public Tilemap targetTilemap;
    public TileBase selectedTile;
    public GameObject mouseIndicator;

    [Header("Camera")]
    [SerializeField] private Camera sceneCamera;

    [Header("Tile Rules")]
    public List<TilePlacementRule> tileRules;

    private bool isDragging = false;
    public bool editMode = true;
    public bool isDeleteTile = false;
    public bool canPlaceTile = false;

    private Vector3Int lastPlacedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    private Vector3Int lastErasedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
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

        if (targetTilemap == null)
            return;

        Vector3 mouseWorldPos = sceneCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = targetTilemap.WorldToCell(mouseWorldPos);

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            if (isDeleteTile)
                EraseTile(cellPosition);
            else
                TryPlaceTile(cellPosition);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            if (isDeleteTile)
                EraseTile(cellPosition);
            else
                TryPlaceTile(cellPosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
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
        else
        {
            Debug.Log("Can't place on this tile");
        }
    }


    private void PlaceTile(Vector3Int cellPosition)
    {
        targetTilemap.SetTile(cellPosition, selectedTile);
        lastPlacedCell = cellPosition;
        Debug.Log("Place tile at: " + cellPosition);
    }

    private void EraseTile(Vector3Int cellPosition)
    {
        if (cellPosition == lastErasedCell)
            return;

        targetTilemap.SetTile(cellPosition, null);
        lastErasedCell = cellPosition;
    }

    public void ToggleDeleteTileMode()
    {
        isDeleteTile = !isDeleteTile;
    }

    public void TogglePlaceTileMode()
    {
        isDeleteTile = false;
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

        foreach (var tilemap in this.tilemap)
        {
            if (rule.requiredBaseTilemapNames.Contains(tilemap.name))
            {
                if (tilemap.GetTile(cellPos) != null)
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

}
