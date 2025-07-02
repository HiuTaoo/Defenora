using UnityEngine;

public class BuildingGhostPreviewSystem : MonoBehaviour
{
    public static BuildingGhostPreviewSystem Instance;

    [Header("Ghost Settings")]
    public float cellSize = 1f;

    public LayerMask placementLayerMask;
    private SpriteRenderer spriteRenderer;

    private GameObject currentGhost;

    private bool canPlace = false;
    private int currentSelectedLayerIndex = 0;

    private GridManager gridManager;
    private LayerManager layerManager;
    public MenuItem menuItem;
    private BuildingFootprint currentFootprint;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        layerManager = FindObjectOfType<LayerManager>();

        if (layerManager != null)
            layerManager.OnLayerIndexChange += HandleLayerIndexChange;

    }

    void Update()
    {
        if (currentGhost == null)
            return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2Int anchorCell = WorldToCell(mouseWorldPos);

        Vector3 anchorWorld = CellToWorld(anchorCell);
        currentGhost.transform.position = anchorWorld;

        canPlace = ValidateFootprint(anchorCell);

        UpdateGhostVisual(canPlace);

        if (canPlace && Input.GetMouseButtonDown(0))
        {
            PlaceBuilding(anchorCell);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelGhost();
        }
    }

    /// <summary>
    /// Hàm xử lý khi MenuItem click → spawn ghost với prefab nhận được.
    /// </summary>
    private void HandleGhostPreview(Transform buildingPrefab)
    {
        if (buildingPrefab == null)
        {
            Debug.LogWarning("Prefab null!");
            return;
        }
        SpawnGhost(buildingPrefab.gameObject);
    }

    public void SpawnGhost(GameObject ghostPrefabToSpawn)
    {
        CancelGhost();

        currentGhost = Instantiate(ghostPrefabToSpawn);
        currentFootprint = currentGhost.GetComponent<BuildingFootprint>();
        spriteRenderer = currentGhost.GetComponent<SpriteRenderer>();
        if (currentFootprint == null)
            Debug.LogError("Ghost prefab thiếu BuildingFootprint component.");
    }

    public void CancelGhost()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
    }

    private bool ValidateFootprint(Vector2Int anchorCell)
    {
        return gridManager.CanPlaceFootprint(anchorCell, currentFootprint, currentSelectedLayerIndex);
    }


    private void PlaceBuilding(Vector2Int anchorCell)
    {
        gridManager.PlaceBuilding(anchorCell, currentFootprint);
        Debug.Log("Placed building at: " + anchorCell);
        //CancelGhost();
    }

    private void UpdateGhostVisual(bool isValid)
    {
        Color color = isValid ? new Color(0, 1, 0, 0.75f) : new Color(1, 0, 0, 0.7f);
        foreach (var renderer in currentGhost.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.color = color;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreen = Input.mousePosition;
        Vector3 world = Camera.main.ScreenToWorldPoint(mouseScreen);
        world.z = 0f;
        return world;
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int y = Mathf.RoundToInt(worldPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    private Vector3 CellToWorld(Vector2Int cellPos)
    {
        float halfCell = cellSize * 0.5f;
        return new Vector3(
            cellPos.x * cellSize + halfCell,
            cellPos.y * cellSize + halfCell,
            0f
        );
    }

    public void RegisterMenuItem(MenuItem item)
    {
        item.OnMenuItemClicked += HandleGhostPreview;
    }

    public void SaveChange()
    {
    }

    public void HandleLayerIndexChange(int layer)
    {
        currentSelectedLayerIndex = layer;
        if(spriteRenderer != null)
            spriteRenderer.sortingOrder = (100 * layer) + 1;
    }
}
