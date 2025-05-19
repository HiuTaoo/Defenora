using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    [SerializeField]
    private Camera sceneCamera;

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

        if (targetTilemap == null )
            return;

        Vector3 mouseWorldPos = sceneCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = targetTilemap.WorldToCell(mouseWorldPos);

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            if (isDeleteTile)
            {
                EraseTile(cellPosition);
            }
            else
            {
                if (selectedTile == null) return;
                if(canPlaceTile)
                    PlaceTile(cellPosition);
            }
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            if (isDeleteTile)
            {
                EraseTile(cellPosition);
            }
            else
            {
                if (canPlaceTile)
                    PlaceTile(cellPosition);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }   
    }

    private void PlaceTile(Vector3Int cellPosition)
    {
        if (cellPosition == lastPlacedCell)
            return;

        targetTilemap.SetTile(cellPosition, selectedTile);
        lastPlacedCell = cellPosition;
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

}
