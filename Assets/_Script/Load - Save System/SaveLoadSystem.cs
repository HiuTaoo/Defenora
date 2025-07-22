using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

public class SaveLoadSystem : MonoBehaviour, ISaveable
{
    public static SaveLoadSystem Instance;
    public List<ISaveable> saveables = new List<ISaveable>();

    private string saveFilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    private UnitManager unitManager;

    public System.Action OnSave;

    [Header("Auto Save Settings")]
    public bool autoSave = true;
    public float autoSaveInterval = 30f;
    private float lastAutoSaveTime = 0f;

    [Header("Load Optimization")]
    public int objectsPerFrame = 50; 
    public bool useObjectPooling = true;
    public bool loadAsync = true;

    private Dictionary<GameObject, Queue<GameObject>> objectPools = new Dictionary<GameObject, Queue<GameObject>>();

    public System.Action OnLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        unitManager = FindObjectOfType<UnitManager>();
        InitializeObjectPools();
    }

    void Start()
    {
        saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();
        PrewarmPools(); 

        if (loadAsync)
            StartCoroutine(LoadGameAsync());
        else
            LoadGame();
    }

    private void LateUpdate()
    {
        if (autoSave && Time.time - lastAutoSaveTime > autoSaveInterval 
            && GameLoop.Instance.StateMachine.CurrentStateType == GameStateType.Playing)
        {
            SaveGame();
            lastAutoSaveTime = Time.time;
            Debug.Log($"Auto-saved game.");
        }
    }

    #region Object Pooling
    private void InitializeObjectPools()
    {
        if (!useObjectPooling) return;

        var spawnSettings = ObjectSpawner.Instance?.spawnSettings;
        if (spawnSettings != null)
        {
            InitializePool(spawnSettings.treePrefabs);
            InitializePool(spawnSettings.bushPrefabs);
            InitializePool(spawnSettings.rockPrefabs);
            InitializePool(spawnSettings.animalPrefabs);
        }
    }

    private void InitializePool(GameObject[] prefabs)
    {
        foreach (var prefab in prefabs)
        {
            if (prefab != null)
                objectPools[prefab] = new Queue<GameObject>();
        }
    }

    public GameObject GetFromPool(GameObject prefab)
    {
        if (!useObjectPooling || !objectPools.ContainsKey(prefab))
            return Instantiate(prefab);

        var pool = objectPools[prefab];
        if (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return Instantiate(prefab);
    }

    public void ReturnToPool(GameObject obj, GameObject prefab)
    {
        if (!useObjectPooling || !objectPools.ContainsKey(prefab))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        objectPools[prefab].Enqueue(obj);
    }

    private void PrewarmPools()
    {
        if (!useObjectPooling) return;

        var spawnSettings = ObjectSpawner.Instance?.spawnSettings;
        if (spawnSettings != null)
        {
            PrewarmPool(spawnSettings.treePrefabs, 20);
            PrewarmPool(spawnSettings.bushPrefabs, 30);
            PrewarmPool(spawnSettings.rockPrefabs, 15);
            PrewarmPool(spawnSettings.animalPrefabs, 5);
        }
    }

    private void PrewarmPool(GameObject[] prefabs, int count)
    {
        foreach (var prefab in prefabs)
        {
            if (prefab != null && objectPools.ContainsKey(prefab))
            {
                for (int i = 0; i < count; i++)
                {
                    var obj = Instantiate(prefab);
                    obj.SetActive(false);
                    objectPools[prefab].Enqueue(obj);
                }
            }
        }
    }
    #endregion

    public void SaveGame()
    {
        OnSave?.Invoke();

        GameSaveData saveData = new GameSaveData();

        foreach (var saveable in saveables)
        {
            saveable.PopulateSaveData(saveData);
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, json);

        Debug.Log($"Game saved to {saveFilePath}");
    }

    public IEnumerator LoadGameAsync()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found!");
            yield break;
        }

        Debug.Log("Starting async load...");

        string json = File.ReadAllText(saveFilePath);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        foreach (var saveable in saveables)
        {
            saveable.LoadFromSaveData(saveData);
            yield return null; 
        }

        yield return StartCoroutine(LoadSpawnDataOptimized(saveData));

        UnitManager.Instance.UpdateGraphNodeWhenStart();

        Debug.Log($"Game loaded asynchronously from {saveFilePath}");
        OnLoaded?.Invoke();
    }

    public void LoadGame()
    {
        if (loadAsync)
        {
            StartCoroutine(LoadGameAsync());
            return;
        }

        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found!");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        foreach (var saveable in saveables)
        {
            saveable.LoadFromSaveData(saveData);
        }

        UnitManager.Instance.UpdateGraphNodeWhenStart();

        Debug.Log($"Game loaded from {saveFilePath}");
        OnLoaded?.Invoke();
    }

    #region Save/ Load Game
    public void PopulateSaveData(GameSaveData saveData)
    {
        #region Save Unit Data
        var unitData = new UnitSaveData();
        foreach (var unit in unitManager.allUnits)
        {
            unitData.units.Add(new UnitData
            {
                unitName = unit.unitName,
                unitType = unit.unitType,
                position = unit.transform.position,
                assignedBuilding = unit.assignedBuilding?.name,
                currentState = unit.currentState,
                health = unit.health,
                layerIndex = unit.floorAgent.currentFloorIndex,
                maxHealth = unit.maxHealth
            });
        }

        saveData.unitSaveData = unitData;
        #endregion

        #region Save Building Data
        var buildingData = new BuildingSaveData();
        foreach (var building in unitManager.buildings)
        {
            buildingData.buildings.Add(new BuildingData
            {
                buildingName = building.name,
                currentCapacity = building.currentCapacity,
                maxCapacity = building.maxCapacity,
                layerIndex = building.LayerIndex,
                archerPositions = building.listArcherPositions,
                buildingType = building.buildingType,
                position = building.transform.position,
                buildingState = building.buildingState,
                maxHealth = building.maxHealth,
                currentHealth = building.currentHealth,
                unitNames = building.stationedUnits
                    .Where(unit => unit != null)
                    .Select(unit => unit.unitName)
                    .ToList()
            });
        }
        #endregion

        #region Save Object Spawn Data
        SaveSpawnData(saveData);
        #endregion

        saveData.buildingSaveData = buildingData;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        #region Load Building
        var buildingData = saveData.buildingSaveData;

        foreach (var building in unitManager.buildings)
            Destroy(building.gameObject);
        unitManager.buildings.Clear();

        foreach (var buildingDatum in buildingData.buildings)
        {
            Building building = unitManager.CreateBuilding(buildingDatum.buildingType, buildingDatum.position);

            #region Load Data
            building.name = buildingDatum.buildingName;
            building.buildingName = buildingDatum.buildingName;
            building.LayerIndex = buildingDatum.layerIndex;
            building.buildingState = buildingDatum.buildingState;
            building.maxCapacity = buildingDatum.maxCapacity;
            building.currentCapacity = buildingDatum.currentCapacity;
            building.maxHealth = buildingDatum.maxHealth;
            building.currentHealth = buildingDatum.currentHealth;
            building.GetSpriteRendererComponent().sortingOrder = RenderManager.Instance.decorRenderIndex;
            #endregion

            var customRender = building.transform.Find("Custom Render Sprite");
            if (customRender != null)
            {
                customRender.GetComponent<CustomRender>().layerIndex = building.LayerIndex;
            }
        }
        #endregion

        #region Load Unit
        var unitData = saveData.unitSaveData;

        foreach (var unit in unitManager.allUnits)
            Destroy(unit.gameObject);
        unitManager.allUnits.Clear();

        foreach (var unitDatum in unitData.units)
        {
            Unit unit = unitManager.CreateUnit(unitDatum.unitType, unitDatum.position);
            unit.unitName = unitDatum.unitName;
            unit.gameObject.name = unitDatum.unitName;
            unit.floorAgent.MoveToFloor(unitDatum.layerIndex);
            foreach (var building in unitManager.buildings) {
                if(building.name == unitDatum.assignedBuilding)
                {
                    unit.assignedBuilding = building;
                    building.stationedUnits.Add(unit);

                    foreach (var spot in building.positionSpots)
                    {
                        if(unit.transform.position == spot.position)
                            building.listArcherPositions.Add(new SpotData { position = spot.position, unitName = unit.unitName });
                        break;
                    }
                    break;
                }
            }
        }
        #endregion
    }

    #region SAVE/LOAD Spawn Object - Optimized
    public void SaveSpawnData(GameSaveData gameSaveData)
    {
        try
        {
            ObjectSpawnData saveData = new ObjectSpawnData();

            foreach (var layerKvp in ObjectSpawner.Instance.layerClusters)
            {
                int layerIndex = layerKvp.Key;
                List<TreeCluster> clusters = layerKvp.Value;

                LayerSpawnData layerData = new LayerSpawnData();
                layerData.layerIndex = layerIndex;

                foreach (var cluster in clusters)
                {
                    layerData.clusters.Add(new TreeClusterData(cluster));
                }

                SaveObjectsOfType(layerIndex, clusters, layerData);

                saveData.layerData.Add(layerData);
            }
            gameSaveData.objectSpawnData = saveData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save spawn data: {e.Message}");
        }
    }

    private void SaveObjectsOfType(int layerIndex, List<TreeCluster> clusters, LayerSpawnData layerData)
    {
        if (ObjectSpawner.Instance.spawnedTrees.TryGetValue(layerIndex, out List<SpawnedTree> trees))
        {
            foreach (var tree in trees)
            {
                if (tree.treeComponent != null)
                {
                    int prefabIndex = GetPrefabIndex(tree.treeComponent.gameObject, ObjectSpawner.Instance.spawnSettings.treePrefabs);
                    int clusterIndex = clusters.IndexOf(tree.parentCluster);
                    layerData.trees.Add(new SpawnedTreeData(tree, prefabIndex, clusterIndex));
                }
            }
        }

        if (ObjectSpawner.Instance.spawnedBushes.TryGetValue(layerIndex, out List<SpawnedBush> bushes))
        {
            foreach (var bush in bushes)
            {
                if (bush.bushObject != null)
                {
                    int prefabIndex = GetPrefabIndex(bush.bushObject, ObjectSpawner.Instance.spawnSettings.bushPrefabs);
                    int clusterIndex = bush.parentCluster != null ? clusters.IndexOf(bush.parentCluster) : -1;
                    layerData.bushes.Add(new SpawnedBushData(bush, prefabIndex, clusterIndex));
                }
            }
        }

        if (ObjectSpawner.Instance.spawnedRocks.TryGetValue(layerIndex, out List<SpawnedRock> rocks))
        {
            foreach (var rock in rocks)
            {
                if (rock.rockObject != null)
                {
                    int prefabIndex = GetPrefabIndex(rock.rockObject, ObjectSpawner.Instance.spawnSettings.rockPrefabs);
                    int clusterIndex = rock.parentCluster != null ? clusters.IndexOf(rock.parentCluster) : -1;
                    layerData.rocks.Add(new SpawnedRockData(rock, prefabIndex, clusterIndex));
                }
            }
        }

        if (ObjectSpawner.Instance.spawnedAnimals.TryGetValue(layerIndex, out List<SpawnedAnimal> animals))
        {
            foreach (var animal in animals)
            {
                if (animal.animalObject != null && animal.animalComponent != null)
                {
                    int prefabIndex = GetPrefabIndex(animal.animalObject, ObjectSpawner.Instance.spawnSettings.animalPrefabs);
                    layerData.animals.Add(new SpawnedAnimalData(animal, prefabIndex));
                }
            }
        }
    }

    public IEnumerator LoadSpawnDataOptimized(GameSaveData gameSaveData)
    {
        if (gameSaveData?.objectSpawnData == null)
        {
            Debug.LogError("Failed to load spawn data: objectSpawnData is null");
            yield break;
        }

        ObjectSpawnData saveData = gameSaveData.objectSpawnData;
        ObjectSpawner.Instance.ClearAllObjects();

        int processedCount = 0;

        foreach (var layerData in saveData.layerData)
        {
            yield return StartCoroutine(LoadLayerWithErrorHandling(layerData));

            processedCount++;
            if (processedCount % 2 == 0) 
                yield return null;
        }
    }

    private IEnumerator LoadLayerWithErrorHandling(LayerSpawnData layerData)
    {
        bool success = false;
        try
        {
            yield return StartCoroutine(LoadLayerDataOptimized(layerData));
            success = true;
        }
        finally
        {
            if (!success)
            {
                Debug.LogError($"Failed to load layer data for layer {layerData?.layerIndex}");
            }
        }
    }

    public void LoadSpawnData(GameSaveData gameSaveData)
    {
        if (loadAsync)
        {
            StartCoroutine(LoadSpawnDataOptimized(gameSaveData));
            return;
        }

        try
        {
            ObjectSpawnData saveData = gameSaveData.objectSpawnData;
            ObjectSpawner.Instance.ClearAllObjects();

            foreach (var layerData in saveData.layerData)
            {
                LoadLayerData(layerData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load spawn data: {e.Message}");
        }
    }

    private IEnumerator LoadLayerDataOptimized(LayerSpawnData layerData)
    {
        int layerIndex = layerData.layerIndex;

        #region Load Clusters
        List<TreeCluster> clusters = new List<TreeCluster>();
        foreach (var clusterData in layerData.clusters)
        {
            clusters.Add(clusterData.ToTreeCluster());
        }
        ObjectSpawner.Instance.layerClusters[layerIndex] = clusters;
        #endregion

        yield return StartCoroutine(LoadObjectsOptimized<SpawnedTreeData, SpawnedTree>(
            layerData.trees,
            (data) => LoadTree(data, clusters),
            (list) => ObjectSpawner.Instance.spawnedTrees[layerIndex] = list));

        yield return StartCoroutine(LoadObjectsOptimized<SpawnedBushData, SpawnedBush>(
            layerData.bushes,
            (data) => LoadBush(data, clusters),
            (list) => ObjectSpawner.Instance.spawnedBushes[layerIndex] = list));

        yield return StartCoroutine(LoadObjectsOptimized<SpawnedRockData, SpawnedRock>(
            layerData.rocks,
            (data) => LoadRock(data, clusters),
            (list) => ObjectSpawner.Instance.spawnedRocks[layerIndex] = list));

        yield return StartCoroutine(LoadObjectsOptimized<SpawnedAnimalData, SpawnedAnimal>(
            layerData.animals,
            (data) => LoadAnimal(data),
            (list) => ObjectSpawner.Instance.spawnedAnimals[layerIndex] = list));
    }

    private IEnumerator LoadObjectsOptimized<TData, TObject>(
        List<TData> dataList,
        System.Func<TData, TObject> loadFunction,
        System.Action<List<TObject>> setResult)
    {
        List<TObject> objects = new List<TObject>();
        int processedCount = 0;

        foreach (var data in dataList)
        {
            TObject obj = default(TObject);
            try
            {
                obj = loadFunction(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load object: {e.Message}");
                continue;
            }

            if (obj != null)
            {
                objects.Add(obj);
            }

            processedCount++;
            if (processedCount >= objectsPerFrame)
            {
                processedCount = 0;
                yield return null; 
            }
        }

        setResult(objects);
    }

    private void LoadLayerData(LayerSpawnData layerData)
    {
        int layerIndex = layerData.layerIndex;

        #region Load Clusters
        List<TreeCluster> clusters = new List<TreeCluster>();
        foreach (var clusterData in layerData.clusters)
        {
            clusters.Add(clusterData.ToTreeCluster());
        }
        ObjectSpawner.Instance.layerClusters[layerIndex] = clusters;
        #endregion

        #region Load Trees
        List<SpawnedTree> trees = new List<SpawnedTree>();
        foreach (var treeData in layerData.trees)
        {
            SpawnedTree spawnedTree = LoadTree(treeData, clusters);
            if (spawnedTree != null)
            {
                trees.Add(spawnedTree);
            }
        }
        ObjectSpawner.Instance.spawnedTrees[layerIndex] = trees;
        #endregion

        #region Load Bushes
        List<SpawnedBush> bushes = new List<SpawnedBush>();
        foreach (var bushData in layerData.bushes)
        {
            SpawnedBush spawnedBush = LoadBush(bushData, clusters);
            if (spawnedBush != null)
            {
                bushes.Add(spawnedBush);
            }
        }
        ObjectSpawner.Instance.spawnedBushes[layerIndex] = bushes;
        #endregion

        #region Load Rocks
        List<SpawnedRock> rocks = new List<SpawnedRock>();
        foreach (var rockData in layerData.rocks)
        {
            SpawnedRock spawnedRock = LoadRock(rockData, clusters);
            if (spawnedRock != null)
            {
                rocks.Add(spawnedRock);
            }
        }
        ObjectSpawner.Instance.spawnedRocks[layerIndex] = rocks;
        #endregion

        #region Load Animals
        List<SpawnedAnimal> animals = new List<SpawnedAnimal>();
        foreach (var animalData in layerData.animals)
        {
            SpawnedAnimal spawnedAnimal = LoadAnimal(animalData);
            if (spawnedAnimal != null)
            {
                animals.Add(spawnedAnimal);
            }
        }
        ObjectSpawner.Instance.spawnedAnimals[layerIndex] = animals;
        #endregion
    }

    #region Load Object Methods
    private SpawnedTree LoadTree(SpawnedTreeData treeData, List<TreeCluster> clusters)
    {
        if (treeData.prefabIndex < 0 || treeData.prefabIndex >= ObjectSpawner.Instance.spawnSettings.treePrefabs.Length)
        {
            Debug.LogWarning($"Invalid tree prefab index: {treeData.prefabIndex}");
            return null;
        }

        GameObject treePrefab = ObjectSpawner.Instance.spawnSettings.treePrefabs[treeData.prefabIndex];
        Vector3 worldPosition = ObjectSpawner.Instance.GridToWorld(treeData.gridPosition);

        GameObject treeObj = GetFromPool(treePrefab);
        treeObj.transform.position = worldPosition;
        treeObj.transform.rotation = Quaternion.identity;
        treeObj.transform.SetParent(this.transform);

        if (treeObj.TryGetComponent<Tree>(out Tree treeComponent))
        {
            treeComponent.layerIndex = treeData.layerIndex;
            treeComponent.positionInGrid = treeData.gridPosition;
            treeComponent.treeState = treeData.treeState;
            treeComponent.currentChopHit = treeData.currentChopHit;
            treeComponent.maxChopHit = treeData.maxChopHit;

            string layerName = $"Layer {treeData.layerIndex + 1}";
            int layerIndex = LayerMask.NameToLayer(layerName);
            treeObj.layer = layerIndex;

            bool isWalkable = treeComponent.treeState == TreeState.Chopped;

            if (treeComponent.treeState == TreeState.Chopped)
            {
                var customRenderSprite = treeObj.transform.Find("Custom Render Sprite");
                if (customRenderSprite != null)
                    customRenderSprite.gameObject.SetActive(false);
                treeComponent.treeCollider.enabled = false;
            }
            else
                GraphNode.Instance.SetWalkableNode(treeData.gridPosition, treeComponent.layerIndex, isWalkable);
        }

        var customRender = treeObj.transform.Find("Custom Render Sprite");
        if (customRender != null)
        {
            customRender.GetComponent<CustomRender>().layerIndex = treeData.layerIndex;
        }

        var spriteRenderer = treeObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = RenderManager.Instance.decorRenderIndex;
        }

        TreeCluster parentCluster = null;
        if (treeData.parentClusterIndex >= 0 && treeData.parentClusterIndex < clusters.Count)
        {
            parentCluster = clusters[treeData.parentClusterIndex];
        }

        return new SpawnedTree(treeComponent, treeData.gridPosition, treeData.layerIndex, parentCluster);
    }

    private SpawnedBush LoadBush(SpawnedBushData bushData, List<TreeCluster> clusters)
    {
        if (bushData.prefabIndex < 0 || bushData.prefabIndex >= ObjectSpawner.Instance.spawnSettings.bushPrefabs.Length)
        {
            Debug.LogWarning($"Invalid bush prefab index: {bushData.prefabIndex}");
            return null;
        }

        GameObject bushPrefab = ObjectSpawner.Instance.spawnSettings.bushPrefabs[bushData.prefabIndex];
        Vector3 worldPosition = ObjectSpawner.Instance.GridToWorld(bushData.gridPosition);

        GameObject bushObj = GetFromPool(bushPrefab);
        bushObj.transform.position = worldPosition;
        bushObj.transform.rotation = Quaternion.identity;
        bushObj.transform.SetParent(this.transform);

        if (bushObj.TryGetComponent<Bush>(out Bush bushComponent))
        {
            bushComponent.layerIndex = bushData.layerIndex;
            bushComponent.positionInGrid = bushData.gridPosition;

            string layerName = $"Layer {bushData.layerIndex + 1}";
            int layerIndexMask = LayerMask.NameToLayer(layerName);
            bushObj.layer = layerIndexMask;
        }

        var spriteRenderer = bushObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = RenderManager.Instance.decorRenderIndex;

        TreeCluster parentCluster = null;
        if (bushData.parentClusterIndex >= 0 && bushData.parentClusterIndex < clusters.Count)
            parentCluster = clusters[bushData.parentClusterIndex];

        return new SpawnedBush(bushObj, bushData.gridPosition, bushData.layerIndex, parentCluster);
    }

    private SpawnedRock LoadRock(SpawnedRockData rockData, List<TreeCluster> clusters)
    {
        if (rockData.prefabIndex < 0 || rockData.prefabIndex >= ObjectSpawner.Instance.spawnSettings.rockPrefabs.Length)
        {
            Debug.LogWarning($"Invalid rock prefab index: {rockData.prefabIndex}");
            return null;
        }

        GameObject rockPrefab = ObjectSpawner.Instance.spawnSettings.rockPrefabs[rockData.prefabIndex];
        Vector3 worldPosition = ObjectSpawner.Instance.GridToWorld(rockData.gridPosition);

        GameObject rockObj = GetFromPool(rockPrefab);
        rockObj.transform.position = worldPosition;
        rockObj.transform.rotation = Quaternion.identity;
        rockObj.transform.SetParent(this.transform);

        if (rockObj.TryGetComponent<Rock>(out Rock rockComponent))
        {
            rockComponent.layerIndex = rockData.layerIndex;
            rockComponent.positionInGrid = rockData.gridPosition;

            string layerName = $"Layer {rockData.layerIndex + 1}";
            int layerIndexMask = LayerMask.NameToLayer(layerName);
            rockObj.layer = layerIndexMask;
        }

        var customRender = rockObj.transform.Find("Custom Render Sprite");
        if (customRender != null)
        {
            customRender.GetComponent<CustomRender>().layerIndex = rockData.layerIndex;
        }

        var spriteRenderer = rockObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = RenderManager.Instance.decorRenderIndex;
        }

        TreeCluster parentCluster = null;
        if (rockData.parentClusterIndex >= 0 && rockData.parentClusterIndex < clusters.Count)
        {
            parentCluster = clusters[rockData.parentClusterIndex];
        }

        return new SpawnedRock(rockObj, rockData.gridPosition, rockData.layerIndex, parentCluster);
    }

    private SpawnedAnimal LoadAnimal(SpawnedAnimalData animalData)
    {
        if (animalData.prefabIndex < 0 || animalData.prefabIndex >= ObjectSpawner.Instance.spawnSettings.animalPrefabs.Length)
        {
            Debug.LogWarning($"Invalid animal prefab index: {animalData.prefabIndex}");
            return null;
        }

        GameObject animalPrefab = ObjectSpawner.Instance.spawnSettings.animalPrefabs[animalData.prefabIndex];
        GameObject animalObj = GetFromPool(animalPrefab);
        animalObj.transform.position = animalData.currentPosition;
        animalObj.transform.rotation = Quaternion.identity;
        animalObj.transform.SetParent(this.transform);

        if (animalObj.TryGetComponent<Animal>(out Animal animalComponent))
        {
            animalComponent.layerIndex = animalData.layerIndex;

            var floorAgent = animalObj.GetComponentInChildren<FloorAgent>();
            if (floorAgent != null)
            {
                floorAgent.MoveToFloor(animalData.layerIndex);
            }
        }

        var spriteRenderer = animalObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = RenderManager.Instance.decorRenderIndex;
        }

        return new SpawnedAnimal(animalObj, Vector3Int.FloorToInt(animalData.currentPosition));
    }
    #endregion

    public int GetPrefabIndex(GameObject gameObject, GameObject[] prefabs)
    {
        string prefabName = gameObject.name.Replace("(Clone)", "");

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i].name == prefabName)
            {
                return i;
            }
        }

        Debug.LogWarning($"Prefab not found for: {prefabName}");
        return 0;
    }

    public bool HasSaveData()
    {
        return File.Exists(saveFilePath);
    }

    public void DeleteSaveData()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("Save data deleted successfully");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete save data: {e.Message}");
        }
    }

    // Cleanup object pools
    private void OnDestroy()
    {
        if (useObjectPooling)
        {
            foreach (var pool in objectPools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            objectPools.Clear();
        }
    }

    #endregion
    #endregion
}