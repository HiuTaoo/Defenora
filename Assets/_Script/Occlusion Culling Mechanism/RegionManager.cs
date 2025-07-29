using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionManager : MonoBehaviour
{
    public static RegionManager Instance;
    [Header("Region Settings")]
    [SerializeField] private Vector2 regionSize = new Vector2(50f, 50f);
    [SerializeField] private Vector2 mapSize = new Vector2(500f, 500f);
    [SerializeField] private Vector2 mapCenter = Vector2.zero;

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cullingBuffer = 20f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color activeRegionColor = Color.green;
    [SerializeField] private Color inactiveRegionColor = Color.red;

    private Dictionary<Vector2Int, MapRegion> regions = new Dictionary<Vector2Int, MapRegion>();
    private HashSet<Vector2Int> activeRegionKeys = new HashSet<Vector2Int>();
    private Vector3 lastCameraPosition;

    public System.Action<MapRegion> OnRegionActivated;
    public System.Action<MapRegion> OnRegionDeactivated;
    

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if(SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.OnLoaded += HandleGameLoaded;
        }
        else { 
            var saveLoadSystem = FindObjectOfType<SaveLoadSystem>();
            SaveLoadSystem.Instance.OnLoaded += HandleGameLoaded;
        }
    }
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        InitializeRegions();
        lastCameraPosition = mainCamera.transform.position;
        UpdateRegions();
    }

    void Update()
    {
        if (Vector3.Distance(mainCamera.transform.position, lastCameraPosition) > 5f)
        {
            UpdateRegions();
            lastCameraPosition = mainCamera.transform.position;
        }
    }

    void InitializeRegions()
    {
        regions.Clear();

        Vector2 startPos = mapCenter - mapSize * 0.5f;
        int regionsX = Mathf.CeilToInt(mapSize.x / regionSize.x);
        int regionsY = Mathf.CeilToInt(mapSize.y / regionSize.y);

        for (int x = 0; x < regionsX; x++)
        {
            for (int y = 0; y < regionsY; y++)
            {
                Vector2 regionCenter = startPos + new Vector2(
                    (x + 0.5f) * regionSize.x,
                    (y + 0.5f) * regionSize.y
                );

                Vector2Int key = new Vector2Int(x, y);
                string regionName = $"Region_{x}_{y}";

                regions[key] = new MapRegion(regionName, regionCenter, regionSize);
            }
        }

        //Debug.Log($"Initialized {regions.Count} regions ({regionsX}x{regionsY})");
    }

    public void RegisterObject(GameObject obj)
    {
        Vector2 objPosition = obj.transform.position;
        Vector2Int regionKey = GetRegionKey(objPosition);

        if (regions.ContainsKey(regionKey))
        {
            regions[regionKey].AddObject(obj);
        }
    }

    public void UnregisterObject(GameObject obj)
    {
        Vector2 objPosition = obj.transform.position;
        Vector2Int regionKey = GetRegionKey(objPosition);

        if (regions.ContainsKey(regionKey))
        {
            regions[regionKey].RemoveObject(obj);
        }
    }

    Vector2Int GetRegionKey(Vector2 worldPosition)
    {
        Vector2 localPos = worldPosition - (mapCenter - mapSize * 0.5f);
        int x = Mathf.FloorToInt(localPos.x / regionSize.x);
        int y = Mathf.FloorToInt(localPos.y / regionSize.y);
        return new Vector2Int(x, y);
    }

    void UpdateRegions()
    {
        Vector2 cameraPos = mainCamera.transform.position;
        float cameraSize = mainCamera.orthographicSize;
        float cameraAspect = mainCamera.aspect;

        Bounds cameraBounds = new Bounds(
            cameraPos,
            new Vector3(
                (cameraSize * cameraAspect + cullingBuffer) * 2f,
                (cameraSize + cullingBuffer) * 2f,
                0f
            )
        );

        HashSet<Vector2Int> newActiveRegions = new HashSet<Vector2Int>();

        foreach (var kvp in regions)
        {
            Vector2Int key = kvp.Key;
            MapRegion region = kvp.Value;

            if (cameraBounds.Intersects(new Bounds(region.bounds.center, region.bounds.size)))
            {
                newActiveRegions.Add(key);
            }
        }

        foreach (var key in activeRegionKeys)
        {
            if (!newActiveRegions.Contains(key))
            {
                regions[key].SetActive(false);
                OnRegionDeactivated?.Invoke(regions[key]);
            }
        }

        foreach (var key in newActiveRegions)
        {
            if (!activeRegionKeys.Contains(key))
            {
                regions[key].SetActive(true);
                OnRegionActivated?.Invoke(regions[key]);
            }
        }

        activeRegionKeys = newActiveRegions;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        foreach (var kvp in regions)
        {
            MapRegion region = kvp.Value;
            Gizmos.color = region.isActive ? activeRegionColor : inactiveRegionColor;
            Gizmos.DrawWireCube(region.bounds.center, region.bounds.size);

            if (region.isActive)
            {
                Gizmos.color = new Color(activeRegionColor.r, activeRegionColor.g, activeRegionColor.b, 0.2f);
                Gizmos.DrawCube(region.bounds.center, region.bounds.size);
            }
        }

        if (mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 cameraPos = mainCamera.transform.position;
            float cameraSize = mainCamera.orthographicSize;
            float cameraAspect = mainCamera.aspect;

            Vector3 size = new Vector3(
                (cameraSize * cameraAspect + cullingBuffer) * 2f,
                (cameraSize + cullingBuffer) * 2f,
                0f
            );

            Gizmos.DrawWireCube(cameraPos, size);
        }
    }

    #region Ultility Methods
    public MapRegion GetRegionAtPosition(Vector2 position)
    {
        Vector2Int key = GetRegionKey(position);
        return regions.ContainsKey(key) ? regions[key] : null;
    }

    public int GetActiveRegionCount()
    {
        return activeRegionKeys.Count;
    }

    public int GetTotalObjectCount()
    {
        int total = 0;
        foreach (var region in regions.Values)
        {
            total += region.objectsInRegion.Count;
        }
        return total;
    }

    public int GetActiveObjectCount()
    {
        int total = 0;
        foreach (var key in activeRegionKeys)
        {
            if (regions.ContainsKey(key))
            {
                total += regions[key].objectsInRegion.Count;
            }
        }
        return total;
    }
    #endregion

    #region Event Handlers
    public void HandleGameLoaded()
    {
        StartCoroutine(LoadRegion());
    }

    private IEnumerator LoadRegion()
    {
        yield return new WaitForSeconds(0.1f);
        Vector2 cameraPos = mainCamera.transform.position;
        float cameraSize = mainCamera.orthographicSize;
        float cameraAspect = mainCamera.aspect;

        Bounds cameraBounds = new Bounds(
            cameraPos,
            new Vector3(
                (cameraSize * cameraAspect + cullingBuffer) * 2f,
                (cameraSize + cullingBuffer) * 2f,
                0f
            )
        );

        HashSet<Vector2Int> newActiveRegions = new HashSet<Vector2Int>();

        foreach (var kvp in regions)
        {
            Vector2Int key = kvp.Key;
            MapRegion region = kvp.Value;

            bool shouldBeActive = cameraBounds.Intersects(new Bounds(region.bounds.center, region.bounds.size));

            if (shouldBeActive)
            {
                newActiveRegions.Add(key);
                if (!region.isActive)
                {
                    region.SetActive(true);
                    OnRegionActivated?.Invoke(region);
                }
            }
            else
            {
                if (region.isActive)
                {
                    region.SetActive(false);
                    OnRegionDeactivated?.Invoke(region);
                }
            }
        }

        activeRegionKeys = newActiveRegions;
    }
    #endregion
}