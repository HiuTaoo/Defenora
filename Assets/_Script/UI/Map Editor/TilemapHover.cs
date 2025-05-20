using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapHover : MonoBehaviour
{
    public static TilemapHover Instance;
    [Header("Empty Tilemap")]
    [SerializeField] 
    private Tilemap emptyTilemap;

    [SerializeField]
    private TileBase greenTileBase;
    [SerializeField]
    private TileBase redTileBase;

    [SerializeField]
    private Tilemap[] tilemaps;

    private Camera camera;

    private Vector3Int lastCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    Vector3 mouseWorldPos;
    Vector3Int cellPosition;
    public bool canPlace = true;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(Instance);
        camera = Camera.main;
    }

    private void Update()
    {
        MouseMovement();
    }

    public void MouseMovement()
    {
        if (MenuTilesController.Instance.targetTilemap == null || MenuTilesController.Instance.selectedTile == null)
            return;

        mouseWorldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        cellPosition = emptyTilemap.WorldToCell(mouseWorldPos);

        PlaceTile(cellPosition);
        
    }


    private void PlaceTile(Vector3Int cellPosition)
    {
        mouseWorldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        TileBase currentTile = MenuTilesController.Instance.targetTilemap.GetTile(cellPosition);

        if (!MenuTilesController.Instance.CanPlaceOnThisLayer(cellPosition) ||
            currentTile != null
            && currentTile == MenuTilesController.Instance.selectedTile)
        {
                EraseTile(lastCell);
                emptyTilemap.SetTile(cellPosition, redTileBase);
                lastCell = cellPosition;
                canPlace = false;
                return;
            
        }
        else
        {
            EraseTile(lastCell);
            emptyTilemap.SetTile(cellPosition, greenTileBase);
            lastCell = cellPosition;
            canPlace = true;
        }
        /*foreach (var tilemap in tilemaps)
        {
            cellPosition = tilemap.WorldToCell(mouseWorldPos);
            if (tilemap.HasTile(cellPosition))
            {
                EraseTile(lastCell);
                emptyTilemap.SetTile(cellPosition, redTileBase);
                lastCell = cellPosition;
                canPlace = false;
                return;
            }
            else
            {
                EraseTile(lastCell);
                emptyTilemap.SetTile(cellPosition, greenTileBase);
                lastCell = cellPosition;
                canPlace = true;
            }
        }*/
    }

    private void EraseTile(Vector3Int cellPosition)
    {
        emptyTilemap.SetTile(cellPosition, null);
        lastCell = cellPosition;
    }
}
