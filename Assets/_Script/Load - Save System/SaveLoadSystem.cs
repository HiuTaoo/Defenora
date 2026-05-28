using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections;
using _Script.Data;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using _Script.Unit_Management_System.Building;

public class SaveLoadSystem : MonoBehaviour, ISaveable
{
    public static SaveLoadSystem Instance;

    private List<ISaveable> saveables = new List<ISaveable>();

    private string saveFilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    private UnitManager unitManager;

    [Header("Auto Save Settings")]
    public bool autoSave = true;
    public float autoSaveInterval = 30f;
    private float lastAutoSaveTime = 0f;

    [Header("Load Optimization")]
    public int objectsPerFrame = 50; 
    public bool useObjectPooling = true;
    public int backgroundObjectsPerFrame = 2;
    public bool loadAsync = true;

    private Transform decorObjectParent;
    
    public System.Action OnLoaded;
    public System.Action OnSave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        unitManager = FindObjectOfType<UnitManager>();
        decorObjectParent = transform.Find("Decor Object");
    }

    void Start()
    {
        saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();

        if (loadAsync)
            StartCoroutine(LoadGameAsync());
        else
            LoadGame();
    }

    private void LateUpdate()
    {
        if (autoSave && Time.time - lastAutoSaveTime > autoSaveInterval 
            && GameManager.Instance.StateMachine.CurrentStateType == GameStateType.Playing)
        {
            //SaveGame();
            lastAutoSaveTime = Time.time;
            //Debug.Log($"Auto-saved game.");
        }
    }

    public void SaveGame()
    {
        OnSave?.Invoke();

        GameSaveData saveData = new GameSaveData();

        foreach (var saveAble in saveables)
        {
            saveAble.PopulateSaveData(saveData);
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

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f; 

        string json = File.ReadAllText(saveFilePath);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        foreach (var saveAble in saveables)
        {
            saveAble.LoadFromSaveData(saveData);
            yield return null; 
        }

        yield return StartCoroutine(LoadSpawnDataGridBased(saveData.objectSpawnData));
    
        LoadTaskDataOnly(saveData);

        Time.timeScale = originalTimeScale > 0 ? originalTimeScale : 1f;
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

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        string json = File.ReadAllText(saveFilePath);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        foreach (var saveAble in saveables)
        {
            saveAble.LoadFromSaveData(saveData);
        }

        LoadTaskDataOnly(saveData);

        UnitManager.Instance.UpdateGraphNodeWhenStart();

        Debug.Log($"Game loaded from {saveFilePath}");

        Time.timeScale = originalTimeScale > 0 ? originalTimeScale : 1f;

        OnLoaded?.Invoke();
    }

    #region Save/ Load Game
    public void PopulateSaveData(GameSaveData saveData)
    {
        saveData.totalCoins = WalletManager.Instance.CurrentCoins;
        
        #region Save Unit Data 
        var unitData = new UnitSaveData();
        foreach (var unit in unitManager.allUnits)
        {
            var unitEntry = new UnitData
            {
                id = unit.GetId(),
                unitName = unit.unitName,
                unitType = unit.unitType,
                position = unit.transform.position,
                assignedBuilding = unit.assignedBuilding?.GetId(),
                currentHealth = unit.health.CurrentHealth,
                layerIndex = unit.floorAgent.currentFloorIndex,
                level = unit.unitStatsManager.currentLevel
            };

            UnitInventory unitInv = unit.GetComponentInChildren<UnitInventory>();
            if (unitInv != null)
            {
                foreach (var slot in unitInv.GetAll())
                {
                    if (slot.itemData != null && slot.amount > 0)
                    {
                        unitEntry.backpackSlots.Add(new SavedInventorySlot 
                        { 
                            itemID = slot.itemData.id, 
                            amount = slot.amount 
                        });
                    }
                }
            }

            unitData.units.Add(unitEntry);
        }
        saveData.unitSaveData = unitData;
        #endregion

        #region Save Building Data 
        var buildingData = new BuildingSaveData();
        foreach (var building in unitManager.buildings)
        {
            var guardComponent = building.gameObject.GetComponent<GuardComponent>();
            var buildingEntry = new BuildingSaveLoadData
            {
                buildingID = building.GetId(),
                buildingName = building.name,
                currentCapacity = building.currentCapacity,
                maxCapacity = building.maxCapacity,
                layerIndex = building.LayerIndex,
                archerPositions = guardComponent == null ? null : guardComponent.listArcherPositions,
                buildingType = building.buildingType,
                position = building.transform.position,
                buildingState = building.buildingState,
                currentHealth = building.health.CurrentHealth,
                unitID = building.stationedUnits
                    .Where(unit => unit != null)
                    .Select(unit => unit.GetId())
                    .ToList()
            };

            if (building is Storage storageBuilding) 
            {
                foreach (var slot in storageBuilding.GetAllSlots()) 
                {
                    if (slot != null && slot.itemData != null && slot.amount > 0)
                    {
                        buildingEntry.storageSlots.Add(new SavedInventorySlot
                        {
                            itemID = slot.itemData.id,
                            amount = slot.amount
                        });
                    }
                }
            }
            
            if (building is Archery archeryBuilding)
            {
                buildingEntry.traineeSlots = archeryBuilding.GetTraineesSaveData();
            }

            buildingData.buildings.Add(buildingEntry);
        }
        saveData.buildingSaveData = buildingData;
        #endregion
        
        #region Save Task Data
        var taskSaveData = new TaskSaveData();
        if (TaskManager.Instance != null)
        {
            foreach (var task in TaskManager.Instance.AllTasks)
            {
                string targetID = "";
                if (task.targetGameObject != null)
                    targetID = task.targetGameObject.GetId();

                List<string> builderIDs = new List<string>();

                var activeBuilders = task.GetBuilders(); 
                
                if (activeBuilders != null)
                {
                    foreach (var builder in activeBuilders)
                    {
                        if (builder != null)
                        {
                            string bID = builder.GetId();
                            if (!string.IsNullOrEmpty(bID))
                            {
                                builderIDs.Add(bID);
                            }
                        }
                    }
                }

                taskSaveData.tasks.Add(new TaskData
                {
                    id = task.id,
                    taskType = task.taskType,
                    layerIndex = task.layerIndex,
                    taskStatus = task.taskStatus,
                    maxBuilders = task.maxBuilders,
                    requiredProgress = task.requiredProgress,
                    currentProgress = task.currentProgress,
                    targetGameObjectID = targetID, 
                    
                    assignedBuilderIDs = builderIDs 
                });
            }
        }
        saveData.taskSaveData = taskSaveData; 
        #endregion

        #region Save Object Spawn Data
        SaveSpawnData(saveData);
        #endregion

        saveData.buildingSaveData = buildingData;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.SetCoinsOnLoad(saveData.totalCoins);
        }
        
        #region Load Building & Storage Content
        var buildingData = saveData.buildingSaveData;

        foreach (var building in unitManager.buildings)
            Destroy(building.gameObject);
        unitManager.buildings.Clear();

        foreach (var buildingDatum in buildingData.buildings)
        {
            Building building = unitManager.CreateBuilding(buildingDatum.buildingType, buildingDatum.position);

            building.OverrideId(buildingDatum.buildingID); 
            building.name = buildingDatum.buildingName;
            building.buildingName = buildingDatum.buildingName;
            building.LayerIndex = buildingDatum.layerIndex;
            building.buildingState = buildingDatum.buildingState;
            building.maxCapacity = buildingDatum.maxCapacity;
            building.currentCapacity = buildingDatum.currentCapacity;
            building.health.SetCurrentHealth(buildingDatum.currentHealth);

            if (building is Storage storageBuilding)
            {
                //storageBuilding.ClearStorage(); 

                foreach (var savedSlot in buildingDatum.storageSlots)
                {
                    ItemData itemSO = SOManager.Instance.GetItemDataById(savedSlot.itemID);
                    if (itemSO != null)
                    {
                        storageBuilding.Add(itemSO, savedSlot.amount); 
                    }
                }
            }

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
            unit.OverrideId(unitDatum.id);
            unit.unitName = unitDatum.unitName;
            unit.unitType = unitDatum.unitType;
            unit.gameObject.name = unitDatum.unitName;
            unit.characterMovement.CurrentLayer = unitDatum.layerIndex;
            unit.floorAgent.MoveToFloor(unitDatum.layerIndex);
            unit.unitStatsManager.SetLevel(unitDatum.level);
            if (unit.health != null && unit.unitStatsManager != null)
            {
                unit.health.maxHealth = unit.unitStatsManager.MaxHealth;
            }
            unit.health.SetCurrentHealth(unitDatum.currentHealth);
            unit.currentState = UnitState.Idle;
            unit.animState = AnimState.Idle;
            
            UnitInventory unitInv = unit.GetComponentInChildren<UnitInventory>();
            if (unitInv != null)
            {
                unitInv.Clear(); 
                foreach (var savedSlot in unitDatum.backpackSlots)
                {
                    ItemData itemSO = SOManager.Instance.GetItemDataById(savedSlot.itemID);
                    if (itemSO != null)
                    {
                        unitInv.Add(itemSO, savedSlot.amount);
                    }
                }
            }
            
            foreach (var building in unitManager.buildings) {
                var guardComponent = building.gameObject.GetComponent<GuardComponent>();
                if(building.GetId() == unitDatum.assignedBuilding)
                {
                    unit.assignedBuilding = building;
                    if (unit.unitType != UnitType.Civilian)
                    {
                        building.stationedUnits.Add(unit);
                    }
                    if (guardComponent != null)
                    {
                        foreach (var spot in guardComponent.positionSpots)
                        {
                            if(unit.transform.position == spot.position)
                                guardComponent.listArcherPositions.Add(new SpotData { position = spot.position, unitName = unit.unitName });
                            break;
                        }
                    }
                    break;
                }
            }
            
            foreach (var building in unitManager.buildings) {
                if(building.GetId() == unitDatum.assignedBuilding)
                {
                    if (building is Archery archeryBuilding)
                    {
                        var savedBuildingData = saveData.buildingSaveData.buildings.FirstOrDefault(b => b.buildingID == building.GetId());
                        var traineeData = savedBuildingData?.traineeSlots.FirstOrDefault(t => t.unitID == unit.GetId());

                        if (traineeData != null)
                        {
                            archeryBuilding.ForceAddTraineeOnLoad(unit, traineeData.Value.currentTrainingHours);
                            break;
                        }
                    }
                }
            }
        }
        #endregion
    }
    
    private void LoadTaskDataOnly(GameSaveData saveData)
    {
        #region Load Task Data (Được gọi sau khi map và unit đã dựng xong)
        if (TaskManager.Instance == null || saveData.taskSaveData == null) return;

        Debug.Log("[Load System] Bắt đầu khôi phục danh sách nhiệm vụ toàn cục...");
        
        var activeTasks = TaskManager.Instance.AllTasks.ToList();
        foreach (var t in activeTasks)
        {
            TaskManager.Instance.RemoveTask(t);
        }

        UniqueId[] allUniqueIdsInScene = FindObjectsOfType<UniqueId>();

        Dictionary<string, Builder> builderLookup = new Dictionary<string, Builder>();
        foreach (var uniqueIdComp in allUniqueIdsInScene)
        {
            if (uniqueIdComp != null && uniqueIdComp.TryGetComponent<Builder>(out Builder builder))
            {
                if (!string.IsNullOrEmpty(uniqueIdComp.Id))
                {
                    builderLookup[uniqueIdComp.Id] = builder;
                }
            }
        }

        List<(Task runtimeTask, List<string> builderIDs)> bindingQueue = new List<(Task, List<string>)>();

        foreach (var taskDatum in saveData.taskSaveData.tasks)
        {
            GameObject targetObj = null;

            if (!string.IsNullOrEmpty(taskDatum.targetGameObjectID))
            {
                UniqueId matchedComponent = allUniqueIdsInScene.FirstOrDefault(u => u.Id == taskDatum.targetGameObjectID);
                if (matchedComponent != null)
                {
                    targetObj = matchedComponent.gameObject;
                }
            }

            Task newTask = new Task(targetObj, taskDatum.taskType, taskDatum.maxBuilders, taskDatum.layerIndex);
            newTask.SetId(taskDatum.id);
            newTask.taskStatus = taskDatum.taskStatus;
            newTask.requiredProgress = taskDatum.requiredProgress;
            newTask.currentProgress = taskDatum.currentProgress;

            TaskManager.Instance.AddTask(newTask);

            if (taskDatum.assignedBuilderIDs != null && taskDatum.assignedBuilderIDs.Count > 0)
            {
                bindingQueue.Add((newTask, taskDatum.assignedBuilderIDs));
            }
        }

        foreach (var binding in bindingQueue)
        {
            Task currentTask = binding.runtimeTask;

            foreach (string builderID in binding.builderIDs)
            {
                if (builderLookup.TryGetValue(builderID, out Builder matchedBuilder))
                {
                    currentTask.ForceAssignBuilderOnLoad(matchedBuilder);

                    matchedBuilder.currentTask = currentTask; //
                    
                    matchedBuilder.targetGO = currentTask.targetGameObject; //

                    Debug.Log($"[Load Thành Công] Đã khôi phục hoàn chỉnh liên kết: Builder [{builderID}] ➔ Task [{currentTask.id}]");
                }
                else
                {
                    Debug.LogWarning($"[Load Thất Bại] Không tìm thấy Builder có ID [{builderID}] trên Scene để gán vào Task!");
                }
            }
        }

        Debug.Log("[Load System] Khôi phục danh sách nhiệm vụ và kích hoạt lại AI thành công!");
        #endregion
    }
    
    
    
    #region SAVE/LOAD Spawn Object 
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
                    string objectId = tree.treeComponent.GetId();
                    int prefabIndex = GetPrefabIndex(tree.treeComponent.gameObject, PrefabConfig.Instance.treePrefabs);
                    int clusterIndex = clusters.IndexOf(tree.parentCluster);
                    layerData.trees.Add(new SpawnedTreeData(tree, prefabIndex, clusterIndex, objectId));
                }
            }
        }

        if (ObjectSpawner.Instance.spawnedBushes.TryGetValue(layerIndex, out List<SpawnedBush> bushes))
        {
            foreach (var bush in bushes)
            {
                if (bush.bushObject != null)
                {
                    string objectId = bush.bushObject.GetId();
                    int prefabIndex = GetPrefabIndex(bush.bushObject, PrefabConfig.Instance.bushPrefabs);
                    int clusterIndex = bush.parentCluster != null ? clusters.IndexOf(bush.parentCluster) : -1;
                    layerData.bushes.Add(new SpawnedBushData(bush, prefabIndex, clusterIndex, objectId));
                }
            }
        }

        if (ObjectSpawner.Instance.spawnedRocks.TryGetValue(layerIndex, out List<SpawnedRock> rocks))
        {
            foreach (var rock in rocks)
            {
                if (rock.rockObject != null)
                {
                    string objectId = rock.rockObject.GetId();
                    int prefabIndex = GetPrefabIndex(rock.rockObject, PrefabConfig.Instance.rockPrefabs);
                    int clusterIndex = rock.parentCluster != null ? clusters.IndexOf(rock.parentCluster) : -1;
                    layerData.rocks.Add(new SpawnedRockData(rock, prefabIndex, clusterIndex, objectId));
                }
            }
        }

        if (ObjectSpawner.Instance.spawnedAnimals.TryGetValue(layerIndex, out List<SpawnedAnimal> animals))
        {
            foreach (var animal in animals)
            {
                if (animal.animalObject != null && animal.animalComponent != null)
                {
                    string objectId = animal.animalObject.GetId();
                    int prefabIndex = GetPrefabIndex(animal.animalObject, PrefabConfig.Instance.animalPrefabs);
                    layerData.animals.Add(new SpawnedAnimalData(animal, prefabIndex, objectId));
                }
            }
        }
    }
    
    private IEnumerator LoadSpawnDataGridBased(ObjectSpawnData saveData)
    {
        if (saveData == null) yield break;

        Dictionary<Vector2Int, List<System.Action<bool>>> regionTasks =
            new Dictionary<Vector2Int, List<System.Action<bool>>>();

        void AddTaskToRegion(Vector3 pos, System.Action<bool> loadTask)
        {
            if (RegionManager.Instance == null)
            {
                Debug.LogError("RegionManager.Instance is null. Cannot group decor objects by region.");
                return;
            }

            Vector2Int key = RegionManager.Instance.GetRegionKey(pos);

            if (!RegionManager.Instance.HasRegion(key))
            {
                Debug.LogWarning($"Object at {pos} is outside map region. Region key: {key}");
                return;
            }

            if (!regionTasks.ContainsKey(key))
                regionTasks[key] = new List<System.Action<bool>>();

            regionTasks[key].Add(loadTask);
        }

        // Phân loại toàn bộ object vào các Region tương ứng
        foreach (var layerData in saveData.layerData)
        {
            int currentLayerIndex = layerData.layerIndex;

            List<TreeCluster> clusters = new List<TreeCluster>();
            foreach (var clusterData in layerData.clusters)
                clusters.Add(clusterData.ToTreeCluster());

            ObjectSpawner.Instance.layerClusters[currentLayerIndex] = clusters;
            ObjectSpawner.Instance.spawnedTrees[currentLayerIndex] = new List<SpawnedTree>();
            ObjectSpawner.Instance.spawnedBushes[currentLayerIndex] = new List<SpawnedBush>();
            ObjectSpawner.Instance.spawnedRocks[currentLayerIndex] = new List<SpawnedRock>();
            ObjectSpawner.Instance.spawnedAnimals[currentLayerIndex] = new List<SpawnedAnimal>();

            var currentClustersRef = ObjectSpawner.Instance.layerClusters[currentLayerIndex];

            foreach (var data in layerData.trees)
            {
                Vector3 worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddTaskToRegion(worldPos, (isHidden) => {
                    var obj = LoadTree(data, currentClustersRef, isHidden);
                    if (obj != null) ObjectSpawner.Instance.spawnedTrees[currentLayerIndex].Add(obj);
                });
            }

            foreach (var data in layerData.bushes)
            {
                Vector3 worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddTaskToRegion(worldPos, (isHidden) => {
                    var obj = LoadBush(data, currentClustersRef, isHidden);
                    if (obj != null) ObjectSpawner.Instance.spawnedBushes[currentLayerIndex].Add(obj);
                });
            }

            foreach (var data in layerData.rocks)
            {
                Vector3 worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddTaskToRegion(worldPos, (isHidden) => {
                    var obj = LoadRock(data, currentClustersRef, isHidden);
                    if (obj != null) ObjectSpawner.Instance.spawnedRocks[currentLayerIndex].Add(obj);
                });
            }

            foreach (var data in layerData.animals)
            {
                Vector3 worldPos = data.currentPosition;
                AddTaskToRegion(worldPos, (isHidden) => {
                    var obj = LoadAnimal(data, isHidden);
                    if (obj != null) ObjectSpawner.Instance.spawnedAnimals[currentLayerIndex].Add(obj);
                });
            }
        }

        yield return new WaitForEndOfFrame();

        // Lấy các Region xung quanh camera trước để ưu tiên hiển thị sạch sẽ
        List<Vector2Int> firstRegionKeys = RegionManager.Instance.GetRegionKeysAroundCamera(true);

        Vector2 cameraPos = Camera.main != null ? (Vector2)Camera.main.transform.position : Vector2.zero;

        // Sắp xếp các Region còn lại từ gần Camera ra xa dần
        List<Vector2Int> remainingRegionKeys = regionTasks.Keys
            .Where(key => !firstRegionKeys.Contains(key))
            .OrderBy(key => ((Vector2)RegionManager.Instance.GetRegionCenter(key) - cameraPos).sqrMagnitude)
            .ToList();

        int processedCount = 0;

        // ĐỒNG BỘ HOÀN TOÀN: Gộp chung cả 2 danh sách Region để load liên tục một mạch cho xong
        List<Vector2Int> allSortedRegionKeys = new List<Vector2Int>();
        allSortedRegionKeys.AddRange(firstRegionKeys);
        allSortedRegionKeys.AddRange(remainingRegionKeys);

        foreach (var key in allSortedRegionKeys)
        {
            if (!regionTasks.ContainsKey(key)) continue;

            // Xác định xem vùng này có nằm ngoài tầm nhìn camera lúc đầu không (để ẩn tối ưu render nếu cần)
            bool isBackground = !firstRegionKeys.Contains(key);

            foreach (var loadTask in regionTasks[key])
            {
                loadTask.Invoke(isBackground);

                processedCount++;
                // Dùng biến objectsPerFrame cấu hình sẵn (Ví dụ: 50 hoặc 100) để load dồn dập
                if (processedCount >= objectsPerFrame)
                {
                    processedCount = 0;
                    // Vì game đang đóng băng thời gian thực (Time.timeScale = 0),
                    // ta dùng WaitForSecondsRealtime để nhường frame cho CPU thở mà không bị kẹt vô tận
                    yield return new WaitForSecondsRealtime(0.001f); 
                }
            }
        }

        // Cập nhật lại sơ đồ di chuyển (A* Graph) sau khi toàn bộ Map và Decor đã dựng xong hoàn chỉnh
        UnitManager.Instance.UpdateGraphNodeWhenStart();
        Debug.Log("[SaveLoadSystem] Đã khôi phục xong toàn bộ thực thể Decor trên bản đồ.");
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
    private SpawnedTree LoadTree(SpawnedTreeData treeData, List<TreeCluster> clusters, bool startHidden = false)
    {
        if (treeData.prefabIndex < 0 || treeData.prefabIndex >= PrefabConfig.Instance.treePrefabs.Length)
        {
            Debug.LogWarning($"Invalid tree prefab index: {treeData.prefabIndex}");
            return null;
        }

        GameObject treePrefab = PrefabConfig.Instance.treePrefabs[treeData.prefabIndex];
        Vector3 worldPosition = ObjectSpawner.Instance.GridToWorld(treeData.gridPosition);

        GameObject treeObj = PoolManager.Instance.Spawn(treePrefab, worldPosition, Quaternion.identity);
        treeObj.transform.SetParent(this.transform);
        treeObj.transform.SetParent(decorObjectParent);
        treeObj.OverrideId(treeData.id);
        
        // 1. Ép bật/tắt cẩn thận (Dọn sạch tàn dư của Object Pool)
        var sr = treeObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = !startHidden;

        var anim = treeObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = !startHidden;

        // 2. ÉP CẬP NHẬT LẠI REGION TẠI VỊ TRÍ MỚI
        var regionObj = treeObj.GetComponent<RegionObject>();
        if (regionObj != null) 
        {
            // Buộc object gỡ đăng ký ở ô cũ và đăng ký vào ô theo toạ độ mới này
            regionObj.UpdateRegion(); 

            // 3. Bảo hiểm: Nếu load ngầm (Phase 2) nhưng camera lỡ quét trúng vùng này
            // -> Ta buộc nó bật lên ngay lập tức để không bao giờ bị tàng hình vĩnh viễn.
            var currentRegion = RegionManager.Instance.GetRegionAtPosition(treeObj.transform.position);
            if (currentRegion != null && currentRegion.isActive)
            {
                regionObj.OnRegionActivated();
            }
        }

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

            if (treeComponent.treeState == TreeState.Chopped)
            {
                var customRenderSprite = treeObj.transform.Find("Custom Render Sprite");
                if (customRenderSprite != null)
                    customRenderSprite.gameObject.SetActive(false);
                //treeComponent.treeCollider.enabled = false;
            }
            GraphNode.Instance.SetWalkableNode(treeData.gridPosition, treeComponent.layerIndex, false);
        }

        var customRender = treeObj.transform.Find("Custom Render Sprite");
        if (customRender != null)
        {
            customRender.GetComponent<CustomRender>().layerIndex = treeData.layerIndex;
        }
        TreeCluster parentCluster = null;
        if (treeData.parentClusterIndex >= 0 && treeData.parentClusterIndex < clusters.Count)
        {
            parentCluster = clusters[treeData.parentClusterIndex];
        }

        return new SpawnedTree(treeComponent, treeData.gridPosition, treeData.layerIndex, parentCluster);
    }

    private SpawnedBush LoadBush(SpawnedBushData bushData, List<TreeCluster> clusters, bool startHidden = false)
    {
        if (bushData.prefabIndex < 0 || bushData.prefabIndex >= PrefabConfig.Instance.bushPrefabs.Length)
        {
            Debug.LogWarning($"Invalid bush prefab index: {bushData.prefabIndex}");
            return null;
        }

        GameObject bushPrefab = PrefabConfig.Instance.bushPrefabs[bushData.prefabIndex];
        Vector3 worldPosition = ObjectSpawner.Instance.GridToWorld(bushData.gridPosition);

        GameObject bushObj = PoolManager.Instance.Spawn(bushPrefab, worldPosition, Quaternion.identity);
        bushObj.transform.SetParent(this.transform);
        bushObj.transform.SetParent(decorObjectParent);
        bushObj.OverrideId(bushData.id);
        
        // 1. Ép bật/tắt cẩn thận (Dọn sạch tàn dư của Object Pool)
        var sr = bushObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = !startHidden;

        var anim = bushObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = !startHidden;

        // 2. ÉP CẬP NHẬT LẠI REGION TẠI VỊ TRÍ MỚI
        var regionObj = bushObj.GetComponent<RegionObject>();
        if (regionObj != null) 
        {
            // Buộc object gỡ đăng ký ở ô cũ và đăng ký vào ô theo toạ độ mới này
            regionObj.UpdateRegion(); 

            // 3. Bảo hiểm: Nếu load ngầm (Phase 2) nhưng camera lỡ quét trúng vùng này
            // -> Ta buộc nó bật lên ngay lập tức để không bao giờ bị tàng hình vĩnh viễn.
            var currentRegion = RegionManager.Instance.GetRegionAtPosition(bushObj.transform.position);
            if (currentRegion != null && currentRegion.isActive)
            {
                regionObj.OnRegionActivated();
            }
        }

        if (bushObj.TryGetComponent<Bush>(out Bush bushComponent))
        {
            bushComponent.layerIndex = bushData.layerIndex;
            bushComponent.positionInGrid = bushData.gridPosition;

            //string layerName = $"Layer {bushData.layerIndex + 1}";
            int layerIndexMask = LayerMask.NameToLayer("Decor");
            bushObj.layer = layerIndexMask;
        }

        TreeCluster parentCluster = null;
        if (bushData.parentClusterIndex >= 0 && bushData.parentClusterIndex < clusters.Count)
            parentCluster = clusters[bushData.parentClusterIndex];

        return new SpawnedBush(bushObj, bushData.gridPosition, bushData.layerIndex, parentCluster);
    }

    private SpawnedRock LoadRock(SpawnedRockData rockData, List<TreeCluster> clusters, bool startHidden = false)
    {
        if (rockData.prefabIndex < 0 || rockData.prefabIndex >= PrefabConfig.Instance.rockPrefabs.Length)
        {
            Debug.LogWarning($"Invalid rock prefab index: {rockData.prefabIndex}");
            return null;
        }

        GameObject rockPrefab = PrefabConfig.Instance.rockPrefabs[rockData.prefabIndex];
        Vector3 worldPosition = ObjectSpawner.Instance.GridToWorld(rockData.gridPosition);
        
        GameObject rockObj = PoolManager.Instance.Spawn(rockPrefab, worldPosition, Quaternion.identity);
        rockObj.transform.SetParent(this.transform);
        rockObj.transform.SetParent(decorObjectParent);
        rockObj.OverrideId(rockData.id);
        
        // 1. Ép bật/tắt cẩn thận (Dọn sạch tàn dư của Object Pool)
        var sr = rockObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = !startHidden;

        var anim = rockObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = !startHidden;

        // 2. ÉP CẬP NHẬT LẠI REGION TẠI VỊ TRÍ MỚI
        var regionObj = rockObj.GetComponent<RegionObject>();
        if (regionObj != null) 
        {
            // Buộc object gỡ đăng ký ở ô cũ và đăng ký vào ô theo toạ độ mới này
            regionObj.UpdateRegion(); 

            // 3. Bảo hiểm: Nếu load ngầm (Phase 2) nhưng camera lỡ quét trúng vùng này
            // -> Ta buộc nó bật lên ngay lập tức để không bao giờ bị tàng hình vĩnh viễn.
            var currentRegion = RegionManager.Instance.GetRegionAtPosition(rockObj.transform.position);
            if (currentRegion != null && currentRegion.isActive)
            {
                regionObj.OnRegionActivated();
            }
        }

        if (rockObj.TryGetComponent<Rock>(out Rock rockComponent))
        {
            rockComponent.layerIndex = rockData.layerIndex;
            rockComponent.positionInGrid = rockData.gridPosition;

            //string layerName = $"Layer {rockData.layerIndex + 1}";
            int layerIndexMask = LayerMask.NameToLayer("Decor");
            rockObj.layer = layerIndexMask;
        }

        var customRender = rockObj.transform.Find("Custom Render Sprite");
        if (customRender != null)
        {
            customRender.GetComponent<CustomRender>().layerIndex = rockData.layerIndex;
        }
        
        TreeCluster parentCluster = null;
        if (rockData.parentClusterIndex >= 0 && rockData.parentClusterIndex < clusters.Count)
        {
            parentCluster = clusters[rockData.parentClusterIndex];
        }

        return new SpawnedRock(rockObj, rockData.gridPosition, rockData.layerIndex, parentCluster);
    }

    private SpawnedAnimal LoadAnimal(SpawnedAnimalData animalData, bool startHidden = false)
    {
        if (animalData.prefabIndex < 0 || animalData.prefabIndex >= PrefabConfig.Instance.animalPrefabs.Length)
        {
            Debug.LogWarning($"Invalid animal prefab index: {animalData.prefabIndex}");
            return null;
        }

        GameObject animalPrefab = PrefabConfig.Instance.animalPrefabs[animalData.prefabIndex];
        GameObject animalObj = PoolManager.Instance.Spawn(animalPrefab, animalData.currentPosition, Quaternion.identity);
        animalObj.transform.SetParent(this.transform);
        animalObj.transform.SetParent(decorObjectParent);
        animalObj.OverrideId(animalData.id);
        
        // 1. Ép bật/tắt cẩn thận (Dọn sạch tàn dư của Object Pool)
        var sr = animalObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = !startHidden;

        var anim = animalObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = !startHidden;

        // 2. ÉP CẬP NHẬT LẠI REGION TẠI VỊ TRÍ MỚI
        var regionObj = animalObj.GetComponent<RegionObject>();
        if (regionObj != null) 
        {
            // Buộc object gỡ đăng ký ở ô cũ và đăng ký vào ô theo toạ độ mới này
            regionObj.UpdateRegion(); 

            // 3. Bảo hiểm: Nếu load ngầm (Phase 2) nhưng camera lỡ quét trúng vùng này
            // -> Ta buộc nó bật lên ngay lập tức để không bao giờ bị tàng hình vĩnh viễn.
            var currentRegion = RegionManager.Instance.GetRegionAtPosition(animalObj.transform.position);
            if (currentRegion != null && currentRegion.isActive)
            {
                regionObj.OnRegionActivated();
            }
        }

        if (animalObj.TryGetComponent<Animal>(out Animal animalComponent))
        {
            animalComponent.layerIndex = animalData.layerIndex;

            var floorAgent = animalObj.GetComponentInChildren<FloorAgent>();
            if (floorAgent != null)
            {
                floorAgent.MoveToFloor(animalData.layerIndex);
            }
        }
        return new SpawnedAnimal(animalObj, Vector3Int.FloorToInt(animalData.currentPosition));
    }
    #endregion

    public int GetPrefabIndex(GameObject gameObj, GameObject[] prefabs)
    {
        string prefabName = gameObj.name.Replace("(Clone)", "");

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
    #endregion
    #endregion

    #region Helper
    private Bounds GetCameraBounds()
    {
        Camera cam = Camera.main;
        if (cam == null) return new Bounds(Vector3.zero, new Vector3(100, 100, 0));

        float cameraSize = cam.orthographicSize;
        float cameraAspect = cam.aspect;
        float cullingBuffer = 20f; 

        return new Bounds(
            cam.transform.position,
            new Vector3(
                (cameraSize * cameraAspect + cullingBuffer) * 2f,
                (cameraSize + cullingBuffer) * 2f,
                0f
            )
        );
    }
    

    #endregion
}