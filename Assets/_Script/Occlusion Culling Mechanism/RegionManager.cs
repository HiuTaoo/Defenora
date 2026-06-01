using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private float checkInterval = 0.2f; 
    
    private float checkTimer; 

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color activeRegionColor = Color.green;
    [SerializeField] private Color inactiveRegionColor = Color.red;

    private Dictionary<Vector2Int, MapRegion> regions = new Dictionary<Vector2Int, MapRegion>();
    private HashSet<Vector2Int> activeRegionKeys = new HashSet<Vector2Int>();
    private Vector3 lastCameraPosition;

    public Action<MapRegion> OnRegionActivated;
    public Action<MapRegion> OnRegionDeactivated;
    

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; 
        }

        var saveLoad = FindObjectOfType<SaveLoadSystem>();
        if (saveLoad != null)
        {
            saveLoad.OnLoaded += HandleGameLoaded;
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
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;

            if (Vector3.Distance(mainCamera.transform.position, lastCameraPosition) > 0.5f)
            {
                UpdateRegions();
                lastCameraPosition = mainCamera.transform.position;
            }
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

    public Vector2Int GetRegionKey(Vector2 worldPosition)
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
    
    public bool HasRegion(Vector2Int key)
    {
        return regions.ContainsKey(key);
    }

    public MapRegion GetRegion(Vector2Int key)
    {
        return regions.TryGetValue(key, out MapRegion region) ? region : null;
    }

    public Vector2 GetRegionCenter(Vector2Int key)
    {
        if (regions.TryGetValue(key, out MapRegion region))
            return region.bounds.center;

        return Vector2.zero;
    }

    public List<Vector2Int> GetRegionKeysAroundCamera(bool includeBuffer = true)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        List<Vector2Int> result = new List<Vector2Int>();

        if (mainCamera == null)
            return result;

        Vector2 cameraPos = mainCamera.transform.position;
        float cameraSize = mainCamera.orthographicSize;
        float cameraAspect = mainCamera.aspect;

        float buffer = includeBuffer ? cullingBuffer : 0f;

        Rect cameraRect = new Rect(
            cameraPos.x - cameraSize * cameraAspect - buffer,
            cameraPos.y - cameraSize - buffer,
            cameraSize * cameraAspect * 2f + buffer * 2f,
            cameraSize * 2f + buffer * 2f
        );

        foreach (var kvp in regions)
        {
            Vector2Int key = kvp.Key;
            MapRegion region = kvp.Value;

            Rect regionRect = new Rect(
                region.bounds.min.x,
                region.bounds.min.y,
                region.bounds.size.x,
                region.bounds.size.y
            );

            if (cameraRect.Overlaps(regionRect))
            {
                result.Add(key);
            }
        }

        result = result
            .OrderBy(key => ((Vector2)GetRegionCenter(key) - cameraPos).sqrMagnitude)
            .ToList();

        return result;
    }

    public List<Vector2Int> GetAllRegionKeysOrderedByDistance(Vector2 position)
    {
        return regions.Keys
            .OrderBy(key => ((Vector2)GetRegionCenter(key) - position).sqrMagnitude)
            .ToList();
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

        if (mainCamera == null) mainCamera = Camera.main;
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

            region.isActive = !shouldBeActive;
            region.SetActive(shouldBeActive); 

            if (shouldBeActive)
            {
                newActiveRegions.Add(key);
                OnRegionActivated?.Invoke(region);
            }
            else
            {
                OnRegionDeactivated?.Invoke(region);
            }
        }

        activeRegionKeys = newActiveRegions;
        Debug.Log($"[Region Culling] Đã tối ưu xong tầm nhìn. Hiện có {activeRegionKeys.Count} vùng hoạt động.");
    }
    #endregion
}