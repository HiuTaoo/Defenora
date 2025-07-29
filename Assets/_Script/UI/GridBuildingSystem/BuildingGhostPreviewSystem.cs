using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingGhostPreviewSystem : MonoBehaviour
{
    public static BuildingGhostPreviewSystem Instance;

    [Header("Ghost Settings")]
    public float cellSize = 1f;

    public LayerMask placementLayerMask;
    private SpriteRenderer spriteRenderer;

    public GameObject currentGhost;

    private bool canPlace = false;

    private EditBuildingManager gridManager;
    public MenuItem menuItem;
    private ObjectFootprint currentFootprint;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        gridManager = FindObjectOfType<EditBuildingManager>();


    }

    void Update()
    {
        if (currentGhost == null)
            return;

        CheckCanPlace();
        
        CheckMouseIsOnUI();
    }

    private void CheckCanPlace()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2Int anchorCell = WorldToCell(mouseWorldPos);

        Vector3 anchorWorld = CellToWorld(anchorCell);
        currentGhost.transform.position = anchorWorld;

        canPlace = ValidateFootprint(anchorCell);

        UpdateGhostVisual(canPlace);

        if (canPlace && Input.GetMouseButtonDown(0) && currentGhost.activeInHierarchy)
        {
            PlaceBuilding(anchorCell);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelGhost();
        }
    }

    public void SpawnGhost(GameObject ghostPrefabToSpawn)
    {
        CancelGhost();

        currentGhost = Instantiate(ghostPrefabToSpawn);
        currentFootprint = currentGhost.GetComponent<ObjectFootprint>();
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
        return gridManager.CanPlaceFootprint(anchorCell, currentFootprint, LayerManager.Instance.layerIndex);
    }


    private void PlaceBuilding(Vector2Int anchorCell)
    {
        gridManager.PlaceBuilding(anchorCell, currentFootprint);
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

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.y / cellSize);
        return new Vector2Int(x, y);
    }


    public Vector3 CellToWorld(Vector2Int cellPos)
    {
        float cellSize = 1f;

        return new Vector3(
            (cellPos.x + 0.5f) * cellSize,
            (cellPos.y + 0.5f) * cellSize,
            0f
        );
    }


    public void RegisterMenuItem(MenuItem item)
    {
        item.OnMenuItemClicked += HandleGhostPreview;
    }

    /// <summary>
    /// Hàm xử lý khi MenuItem click → spawn ghost với prefab nhận được.
    /// </summary>
    private void HandleGhostPreview(string buildingPrefab)
    {
        GameObject building = UnitManager.Instance.FindBuildingPrefab(buildingPrefab);
        SpawnGhost(building);
    }

    public void CheckMouseIsOnUI()
    {
        bool isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        if (isPointerOverUI)
        {
            currentGhost?.SetActive(false);
            return;
        }
        else
        {
            currentGhost?.SetActive(true);
        }
    }

    public void HandleLayerIndexChange(int layer)
    {
        LayerManager.Instance.layerIndex = layer;
        if(currentGhost != null)
            currentGhost.GetComponent<Building>().UpdateRenderSortingOrder(layer);
    }


}
