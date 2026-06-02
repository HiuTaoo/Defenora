using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Script.Data;
using _Script.ItemScript;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using _Script.Unit_Management_System.Building;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour, ISaveable
{
    public static SaveLoadSystem Instance;

    [Header("Auto Save Settings")] public bool autoSave = true;

    public float autoSaveInterval = 30f;

    [Header("Load Optimization")] public int objectsPerFrame = 50;

    public bool useObjectPooling = true;
    public int backgroundObjectsPerFrame = 2;
    public bool loadAsync = true;

    private Transform decorObjectParent;
    private float lastAutoSaveTime;

    public Action OnLoaded;
    public Action OnSave;

    private List<ISaveable> saveables = new();

    private UnitManager unitManager;

    private string saveFilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

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

    private void Start()
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
            //SaveGame();
            lastAutoSaveTime = Time.time;
        //Debug.Log($"Auto-saved game.");
    }

    public void SaveGame()
    {
        OnSave?.Invoke();

        var saveData = new GameSaveData();

        foreach (var saveAble in saveables) saveAble.PopulateSaveData(saveData);

        var json = JsonUtility.ToJson(saveData, true);
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

        var originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        var json = File.ReadAllText(saveFilePath);
        var saveData = JsonUtility.FromJson<GameSaveData>(json);

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

        var originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        var json = File.ReadAllText(saveFilePath);
        var saveData = JsonUtility.FromJson<GameSaveData>(json);

        foreach (var saveAble in saveables) saveAble.LoadFromSaveData(saveData);

        LoadTaskDataOnly(saveData);

        UnitManager.Instance.UpdateGraphNodeWhenStart();

        Debug.Log($"Game loaded from {saveFilePath}");

        Time.timeScale = originalTimeScale > 0 ? originalTimeScale : 1f;

        OnLoaded?.Invoke();
    }

    #region Helper

    private Bounds GetCameraBounds()
    {
        var cam = Camera.main;
        if (cam == null) return new Bounds(Vector3.zero, new Vector3(100, 100, 0));

        var cameraSize = cam.orthographicSize;
        var cameraAspect = cam.aspect;
        var cullingBuffer = 20f;

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

            var unitInv = unit.GetComponentInChildren<UnitInventory>();
            if (unitInv != null)
                foreach (var slot in unitInv.GetAll())
                    if (slot.itemData != null && slot.amount > 0)
                        unitEntry.backpackSlots.Add(new SavedInventorySlot
                        {
                            itemID = slot.itemData.id,
                            amount = slot.amount
                        });

            unitData.units.Add(unitEntry);
        }

        saveData.unitSaveData = unitData;

        #endregion

        #region Save Building Data

        var buildingData = new BuildingSaveData();
        foreach (var building in unitManager.buildings)
        {
            var guardComponent = building.gameObject.GetComponent<GuardComponent>();
            List<SpotData> savedPositions = null;
            if (guardComponent != null && guardComponent.listArcherPositions != null)
                savedPositions = new List<SpotData>(guardComponent.listArcherPositions);

            var buildingEntry = new BuildingSaveLoadData
            {
                buildingID = building.GetId(),
                buildingName = building.name,
                currentCapacity = building.currentCapacity,
                maxCapacity = building.maxCapacity,
                layerIndex = building.LayerIndex,
                archerPositions = savedPositions,
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
                    if (slot != null && slot.itemData != null && slot.amount > 0)
                        buildingEntry.storageSlots.Add(new SavedInventorySlot
                        {
                            itemID = slot.itemData.id,
                            amount = slot.amount
                        });
            }

            else if (building is TrainingBuilding trainingBuilding)
            {
                buildingEntry.traineeSlots = trainingBuilding.GetTraineesSaveData();
            }

            buildingData.buildings.Add(buildingEntry);
        }

        saveData.buildingSaveData = buildingData;

        #endregion

        #region Save Task Data

        var taskSaveData = new TaskSaveData();
        if (TaskManager.Instance != null)
            foreach (var task in TaskManager.Instance.AllTasks)
            {
                var targetID = "";
                if (task.targetGameObject != null)
                    targetID = task.targetGameObject.GetId();

                var builderIDs = new List<string>();

                var activeBuilders = task.GetBuilders();

                if (activeBuilders != null)
                    foreach (var builder in activeBuilders)
                        if (builder != null)
                        {
                            var bID = builder.GetId();
                            if (!string.IsNullOrEmpty(bID)) builderIDs.Add(bID);
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

        saveData.taskSaveData = taskSaveData;

        #endregion

        #region Save Object Spawn Data

        SaveSpawnData(saveData);

        if (ObjectSpawner.Instance != null) ObjectSpawner.Instance.PopulateSpawnerSaveData(saveData);

        #endregion

        #region Save Shop Data

        if (ShopManager.Instance != null) ShopManager.Instance.PopulateShopSaveData(saveData);

        #endregion
        
        #region Save Ground Items Data
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.PopulateItemSaveData(saveData);
        }
        #endregion
        
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (WalletManager.Instance != null) WalletManager.Instance.SetCoinsOnLoad(saveData.totalCoins);

        #region Load Building & Storage Content

        var buildingData = saveData.buildingSaveData;

        foreach (var building in unitManager.buildings)
            Destroy(building.gameObject);
        unitManager.buildings.Clear();

        foreach (var buildingDatum in buildingData.buildings)
        {
            var building = unitManager.CreateBuilding(buildingDatum.buildingType, buildingDatum.position);

            building.OverrideId(buildingDatum.buildingID);
            building.name = buildingDatum.buildingName;
            building.buildingName = buildingDatum.buildingName;
            building.LayerIndex = buildingDatum.layerIndex;
            building.buildingState = buildingDatum.buildingState;
            building.maxCapacity = buildingDatum.maxCapacity;
            building.currentCapacity = buildingDatum.currentCapacity;
            building.health.SetCurrentHealth(buildingDatum.currentHealth);

            if (building is Storage storageBuilding)
                //storageBuilding.ClearStorage(); 
                foreach (var savedSlot in buildingDatum.storageSlots)
                {
                    var itemSO = SOManager.Instance.GetItemDataById(savedSlot.itemID);
                    if (itemSO != null) storageBuilding.Add(itemSO, savedSlot.amount);
                }

            var customRender = building.transform.Find("Custom Render Sprite");
            if (customRender != null) customRender.GetComponent<CustomRender>().layerIndex = building.LayerIndex;
        }

        #endregion

        #region Load Unit

        var unitData = saveData.unitSaveData;

        foreach (var unit in unitManager.allUnits)
            Destroy(unit.gameObject);
        unitManager.allUnits.Clear();

        foreach (var unitDatum in unitData.units)
        {
            var unit = unitManager.CreateUnit(unitDatum.unitType, unitDatum.position);
            unit.OverrideId(unitDatum.id);
            unit.unitName = unitDatum.unitName;
            unit.unitType = unitDatum.unitType;
            unit.gameObject.name = unitDatum.unitName;
            unit.characterMovement.CurrentLayer = unitDatum.layerIndex;
            unit.floorAgent.MoveToFloor(unitDatum.layerIndex);
            unit.unitStatsManager.SetLevel(unitDatum.level);
            if (unit.health != null && unit.unitStatsManager != null)
                unit.health.maxHealth = unit.unitStatsManager.MaxHealth;
            unit.health.SetCurrentHealth(unitDatum.currentHealth);
            unit.currentState = UnitState.Idle;
            unit.animState = AnimState.Idle;

            var unitInv = unit.GetComponentInChildren<UnitInventory>();
            if (unitInv != null)
            {
                unitInv.Clear();
                foreach (var savedSlot in unitDatum.backpackSlots)
                {
                    var itemSO = SOManager.Instance.GetItemDataById(savedSlot.itemID);
                    if (itemSO != null) unitInv.Add(itemSO, savedSlot.amount);
                }
            }

            foreach (var building in unitManager.buildings)
            {
                if (building.GetId() != unitDatum.assignedBuilding)
                    continue;

                var isAssignedAsTrainee = false;

                // ------------------------------------------------------------
                // NHÁNH 1: Kiểm tra xem Unit này có phải là Học viên đang học dở tại đây không
                // ------------------------------------------------------------
                if (building is TrainingBuilding trainingBuilding)
                {
                    var savedBuildingData = saveData.buildingSaveData.buildings
                        .FirstOrDefault(b => b.buildingID == building.GetId());

                    if (savedBuildingData != null && savedBuildingData.traineeSlots != null)
                    {
                        var hasTrainee = savedBuildingData.traineeSlots.Any(t => t.unitID == unit.GetId());

                        if (hasTrainee)
                        {
                            var traineeData = savedBuildingData.traineeSlots.First(t => t.unitID == unit.GetId());

                            trainingBuilding.ForceAddTraineeOnLoad(unit, traineeData.currentTrainingHours,
                                traineeData.targetType);
                            isAssignedAsTrainee = true;
                        }
                    }
                }

                // ------------------------------------------------------------
                // NHÁNH 2: Nếu KHÔNG phải học viên, tiến hành gán vào lính gác (stationedUnits)
                // ------------------------------------------------------------
                if (!isAssignedAsTrainee)
                {
                    building.ForceAddUnitOnLoad(unit);

                    var guardComponent = building.gameObject.GetComponent<GuardComponent>();
                    if (guardComponent != null)
                    {
                        var savedBuildingData = saveData.buildingSaveData.buildings
                            .FirstOrDefault(b => b.buildingID == unitDatum.assignedBuilding);

                        if (savedBuildingData != null && savedBuildingData.archerPositions != null)
                        {
                            var matchedSpotData = savedBuildingData.archerPositions
                                .FirstOrDefault(s => s.unitId == unit.GetId());

                            if (!string.IsNullOrEmpty(matchedSpotData.unitId))
                            {
                                if (guardComponent.listArcherPositions == null)
                                {
                                    guardComponent.listArcherPositions = new List<SpotData>();
                                }

                                bool isAlreadyRestored = guardComponent.listArcherPositions
                                    .Any(s => s.unitId == unit.GetId());

                                if (!isAlreadyRestored)
                                {
                                    guardComponent.listArcherPositions.Add(new SpotData
                                    {
                                        position = matchedSpotData.position,
                                        unitId = unit.GetId()
                                    });
                                    
                                    unit.transform.position = matchedSpotData.position;
                                }
                            }
                        }
                    }
                }

                break;
            }
        }

        #endregion

        #region Load Shop Data

        if (ShopManager.Instance != null) ShopManager.Instance.LoadShopFromSaveData(saveData);

        #endregion
        
        #region Load Ground Items Data
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.LoadItemsFromSaveData(saveData);
        }
        #endregion

        #region Load Respawn Info

        if (ObjectSpawner.Instance != null) ObjectSpawner.Instance.LoadSpawnerFromSaveData(saveData);

        #endregion
    }

    private void LoadTaskDataOnly(GameSaveData saveData)
    {
        #region Load Task Data (Được gọi sau khi map và unit đã dựng xong)

        if (TaskManager.Instance == null || saveData.taskSaveData == null) return;

        Debug.Log("[Load System] Bắt đầu khôi phục danh sách nhiệm vụ toàn cục...");

        var activeTasks = TaskManager.Instance.AllTasks.ToList();
        foreach (var t in activeTasks) TaskManager.Instance.RemoveTask(t);

        var allUniqueIdsInScene = FindObjectsOfType<UniqueId>();

        var builderLookup = new Dictionary<string, Builder>();
        foreach (var uniqueIdComp in allUniqueIdsInScene)
            if (uniqueIdComp != null && uniqueIdComp.TryGetComponent(out Builder builder))
                if (!string.IsNullOrEmpty(uniqueIdComp.Id))
                    builderLookup[uniqueIdComp.Id] = builder;

        List<(Task runtimeTask, List<string> builderIDs)> bindingQueue = new();

        foreach (var taskDatum in saveData.taskSaveData.tasks)
        {
            GameObject targetObj = null;

            if (!string.IsNullOrEmpty(taskDatum.targetGameObjectID))
            {
                var matchedComponent = allUniqueIdsInScene.FirstOrDefault(u => u.Id == taskDatum.targetGameObjectID);
                if (matchedComponent != null) targetObj = matchedComponent.gameObject;
            }

            var newTask = new Task(targetObj, taskDatum.taskType, taskDatum.maxBuilders, taskDatum.layerIndex);
            newTask.SetId(taskDatum.id);
            newTask.taskStatus = taskDatum.taskStatus;
            newTask.requiredProgress = taskDatum.requiredProgress;
            newTask.currentProgress = taskDatum.currentProgress;

            TaskManager.Instance.AddTask(newTask);

            if (taskDatum.assignedBuilderIDs != null && taskDatum.assignedBuilderIDs.Count > 0)
                bindingQueue.Add((newTask, taskDatum.assignedBuilderIDs));
        }

        foreach (var binding in bindingQueue)
        {
            var currentTask = binding.runtimeTask;

            foreach (var builderID in binding.builderIDs)
                if (builderLookup.TryGetValue(builderID, out var matchedBuilder))
                {
                    currentTask.ForceAssignBuilderOnLoad(matchedBuilder);

                    matchedBuilder.currentTask = currentTask; //

                    matchedBuilder.targetGO = currentTask.targetGameObject; //

                    Debug.Log(
                        $"[Load Thành Công] Đã khôi phục hoàn chỉnh liên kết: Builder [{builderID}] ➔ Task [{currentTask.id}]");
                }
                else
                {
                    Debug.LogWarning(
                        $"[Load Thất Bại] Không tìm thấy Builder có ID [{builderID}] trên Scene để gán vào Task!");
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
            var saveData = new ObjectSpawnData();

            foreach (var layerKvp in ObjectSpawner.Instance.layerClusters)
            {
                var layerIndex = layerKvp.Key;
                var clusters = layerKvp.Value;

                var layerData = new LayerSpawnData();
                layerData.layerIndex = layerIndex;

                foreach (var cluster in clusters) layerData.clusters.Add(new TreeClusterData(cluster));

                SaveObjectsOfType(layerIndex, clusters, layerData);

                saveData.layerData.Add(layerData);
            }

            gameSaveData.objectSpawnData = saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save spawn data: {e.Message}");
        }
    }

    private void SaveObjectsOfType(int layerIndex, List<TreeCluster> clusters, LayerSpawnData layerData)
    {
        if (ObjectSpawner.Instance.spawnedTrees.TryGetValue(layerIndex, out var trees))
            foreach (var tree in trees)
                if (tree.treeComponent != null)
                {
                    var objectId = tree.treeComponent.GetId();
                    var prefabIndex = GetPrefabIndex(tree.treeComponent.gameObject, PrefabConfig.Instance.treePrefabs);
                    var clusterIndex = clusters.IndexOf(tree.parentCluster);
                    layerData.trees.Add(new SpawnedTreeData(tree, prefabIndex, clusterIndex, objectId));
                }

        if (ObjectSpawner.Instance.spawnedBushes.TryGetValue(layerIndex, out var bushes))
            foreach (var bush in bushes)
                if (bush.bushObject != null)
                {
                    var objectId = bush.bushObject.GetId();
                    var prefabIndex = GetPrefabIndex(bush.bushObject, PrefabConfig.Instance.bushPrefabs);
                    var clusterIndex = bush.parentCluster != null ? clusters.IndexOf(bush.parentCluster) : -1;
                    layerData.bushes.Add(new SpawnedBushData(bush, prefabIndex, clusterIndex, objectId));
                }

        if (ObjectSpawner.Instance.spawnedRocks.TryGetValue(layerIndex, out var rocks))
            foreach (var rock in rocks)
                if (rock.rockObject != null)
                {
                    var objectId = rock.rockObject.GetId();
                    var prefabIndex = GetPrefabIndex(rock.rockObject, PrefabConfig.Instance.rockPrefabs);
                    var clusterIndex = rock.parentCluster != null ? clusters.IndexOf(rock.parentCluster) : -1;
                    layerData.rocks.Add(new SpawnedRockData(rock, prefabIndex, clusterIndex, objectId));
                }

        if (ObjectSpawner.Instance.spawnedAnimals.TryGetValue(layerIndex, out var animals))
            foreach (var animal in animals)
                if (animal.animalObject != null && animal.animalComponent != null)
                {
                    var objectId = animal.animalObject.GetId();
                    var prefabIndex = GetPrefabIndex(animal.animalObject, PrefabConfig.Instance.animalPrefabs);
                    layerData.animals.Add(new SpawnedAnimalData(animal, prefabIndex, objectId));
                }
    }

    private IEnumerator LoadSpawnDataGridBased(ObjectSpawnData saveData)
    {
        if (saveData == null) yield break;

        var regionTasks =
            new Dictionary<Vector2Int, List<Action<bool>>>();

        void AddTaskToRegion(Vector3 pos, Action<bool> loadTask)
        {
            if (RegionManager.Instance == null)
            {
                Debug.LogError("RegionManager.Instance is null. Cannot group decor objects by region.");
                return;
            }

            var key = RegionManager.Instance.GetRegionKey(pos);

            if (!RegionManager.Instance.HasRegion(key))
            {
                Debug.LogWarning($"Object at {pos} is outside map region. Region key: {key}");
                return;
            }

            if (!regionTasks.ContainsKey(key))
                regionTasks[key] = new List<Action<bool>>();

            regionTasks[key].Add(loadTask);
        }

        // Phân loại toàn bộ object vào các Region tương ứng
        foreach (var layerData in saveData.layerData)
        {
            var currentLayerIndex = layerData.layerIndex;

            var clusters = new List<TreeCluster>();
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
                var worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddTaskToRegion(worldPos, isHidden =>
                {
                    var obj = LoadTree(data, currentClustersRef, isHidden);
                    if (obj != null) ObjectSpawner.Instance.spawnedTrees[currentLayerIndex].Add(obj);
                });
            }

            foreach (var data in layerData.bushes)
            {
                var worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddTaskToRegion(worldPos, isHidden =>
                {
                    var obj = LoadBush(data, currentClustersRef, isHidden);
                    if (obj != null) ObjectSpawner.Instance.spawnedBushes[currentLayerIndex].Add(obj);
                });
            }

            foreach (var data in layerData.rocks)
            {
                var worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddTaskToRegion(worldPos, isHidden =>
                {
                    var obj = LoadRock(data, currentClustersRef, isHidden);
                    if (obj != null) ObjectSpawner.Instance.spawnedRocks[currentLayerIndex].Add(obj);
                });
            }

            foreach (var data in layerData.animals)
            {
                var worldPos = data.currentPosition;
                AddTaskToRegion(worldPos, isHidden =>
                {
                    var obj = LoadAnimal(data, isHidden);
                    if (obj != null) ObjectSpawner.Instance.spawnedAnimals[currentLayerIndex].Add(obj);
                });
            }
        }

        yield return new WaitForEndOfFrame();

        var firstRegionKeys = RegionManager.Instance.GetRegionKeysAroundCamera();

        var cameraPos = Camera.main != null ? (Vector2)Camera.main.transform.position : Vector2.zero;

        var remainingRegionKeys = regionTasks.Keys
            .Where(key => !firstRegionKeys.Contains(key))
            .OrderBy(key => (RegionManager.Instance.GetRegionCenter(key) - cameraPos).sqrMagnitude)
            .ToList();

        var processedCount = 0;

        var allSortedRegionKeys = new List<Vector2Int>();
        allSortedRegionKeys.AddRange(firstRegionKeys);
        allSortedRegionKeys.AddRange(remainingRegionKeys);

        foreach (var key in allSortedRegionKeys)
        {
            if (!regionTasks.ContainsKey(key)) continue;

            var isBackground = !firstRegionKeys.Contains(key);

            foreach (var loadTask in regionTasks[key])
            {
                loadTask.Invoke(isBackground);

                processedCount++;
                if (processedCount >= objectsPerFrame)
                {
                    processedCount = 0;
                    yield return new WaitForSecondsRealtime(0.001f);
                }
            }
        }

        UnitManager.Instance.UpdateGraphNodeWhenStart();
        Debug.Log("[SaveLoadSystem] Đã khôi phục xong toàn bộ thực thể Decor trên bản đồ.");

        if (ObjectSpawner.Instance != null) ObjectSpawner.Instance.LinkChoppedTreesOnMapLoaded();

        OnLoaded?.Invoke();
    }

    private void LoadLayerData(LayerSpawnData layerData)
    {
        var layerIndex = layerData.layerIndex;

        #region Load Clusters

        var clusters = new List<TreeCluster>();
        foreach (var clusterData in layerData.clusters) clusters.Add(clusterData.ToTreeCluster());
        ObjectSpawner.Instance.layerClusters[layerIndex] = clusters;

        #endregion

        #region Load Trees

        var trees = new List<SpawnedTree>();
        foreach (var treeData in layerData.trees)
        {
            var spawnedTree = LoadTree(treeData, clusters);
            if (spawnedTree != null) trees.Add(spawnedTree);
        }

        ObjectSpawner.Instance.spawnedTrees[layerIndex] = trees;

        #endregion

        #region Load Bushes

        var bushes = new List<SpawnedBush>();
        foreach (var bushData in layerData.bushes)
        {
            var spawnedBush = LoadBush(bushData, clusters);
            if (spawnedBush != null) bushes.Add(spawnedBush);
        }

        ObjectSpawner.Instance.spawnedBushes[layerIndex] = bushes;

        #endregion

        #region Load Rocks

        var rocks = new List<SpawnedRock>();
        foreach (var rockData in layerData.rocks)
        {
            var spawnedRock = LoadRock(rockData, clusters);
            if (spawnedRock != null) rocks.Add(spawnedRock);
        }

        ObjectSpawner.Instance.spawnedRocks[layerIndex] = rocks;

        #endregion

        #region Load Animals

        var animals = new List<SpawnedAnimal>();
        foreach (var animalData in layerData.animals)
        {
            var spawnedAnimal = LoadAnimal(animalData);
            if (spawnedAnimal != null) animals.Add(spawnedAnimal);
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

        var treePrefab = PrefabConfig.Instance.treePrefabs[treeData.prefabIndex];
        var worldPosition = ObjectSpawner.Instance.GridToWorld(treeData.gridPosition);

        var treeObj = PoolManager.Instance.Spawn(treePrefab, worldPosition, Quaternion.identity);
        treeObj.transform.SetParent(decorObjectParent);
        treeObj.OverrideId(treeData.id);

        var sr = treeObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = !startHidden;

        var anim = treeObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = !startHidden;

        var regionObj = treeObj.GetComponent<RegionObject>();
        if (regionObj != null)
        {
            regionObj.UpdateRegion();

            var currentRegion = RegionManager.Instance.GetRegionAtPosition(treeObj.transform.position);
            if (currentRegion != null && currentRegion.isActive) regionObj.OnRegionActivated();
        }

        if (treeObj.TryGetComponent(out Tree treeComponent))
        {
            treeComponent.layerIndex = treeData.layerIndex;
            treeComponent.positionInGrid = treeData.gridPosition;
            treeComponent.treeState = treeData.treeState;
            treeComponent.currentChopHit = treeData.currentChopHit;
            treeComponent.maxChopHit = treeData.maxChopHit;


            var layerName = $"Layer {treeData.layerIndex + 1}";
            var layerIndex = LayerMask.NameToLayer(layerName);
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
        if (customRender != null) customRender.GetComponent<CustomRender>().layerIndex = treeData.layerIndex;
        TreeCluster parentCluster = null;
        if (treeData.parentClusterIndex >= 0 && treeData.parentClusterIndex < clusters.Count)
            parentCluster = clusters[treeData.parentClusterIndex];

        return new SpawnedTree(treeComponent, treeData.gridPosition, treeData.layerIndex, parentCluster);
    }

    private SpawnedBush LoadBush(SpawnedBushData bushData, List<TreeCluster> clusters, bool startHidden = false)
    {
        if (bushData.prefabIndex < 0 || bushData.prefabIndex >= PrefabConfig.Instance.bushPrefabs.Length)
        {
            Debug.LogWarning($"Invalid bush prefab index: {bushData.prefabIndex}");
            return null;
        }

        var bushPrefab = PrefabConfig.Instance.bushPrefabs[bushData.prefabIndex];
        var worldPosition = ObjectSpawner.Instance.GridToWorld(bushData.gridPosition);

        var bushObj = PoolManager.Instance.Spawn(bushPrefab, worldPosition, Quaternion.identity);
        bushObj.transform.SetParent(transform);
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
            if (currentRegion != null && currentRegion.isActive) regionObj.OnRegionActivated();
        }

        if (bushObj.TryGetComponent(out Bush bushComponent))
        {
            bushComponent.layerIndex = bushData.layerIndex;
            bushComponent.positionInGrid = bushData.gridPosition;

            //string layerName = $"Layer {bushData.layerIndex + 1}";
            var layerIndexMask = LayerMask.NameToLayer("Decor");
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

        var rockPrefab = PrefabConfig.Instance.rockPrefabs[rockData.prefabIndex];
        var worldPosition = ObjectSpawner.Instance.GridToWorld(rockData.gridPosition);

        var rockObj = PoolManager.Instance.Spawn(rockPrefab, worldPosition, Quaternion.identity);
        rockObj.transform.SetParent(transform);
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
            if (currentRegion != null && currentRegion.isActive) regionObj.OnRegionActivated();
        }

        if (rockObj.TryGetComponent(out Rock rockComponent))
        {
            rockComponent.layerIndex = rockData.layerIndex;
            rockComponent.positionInGrid = rockData.gridPosition;

            //string layerName = $"Layer {rockData.layerIndex + 1}";
            var layerIndexMask = LayerMask.NameToLayer("Decor");
            rockObj.layer = layerIndexMask;
        }

        var customRender = rockObj.transform.Find("Custom Render Sprite");
        if (customRender != null) customRender.GetComponent<CustomRender>().layerIndex = rockData.layerIndex;

        TreeCluster parentCluster = null;
        if (rockData.parentClusterIndex >= 0 && rockData.parentClusterIndex < clusters.Count)
            parentCluster = clusters[rockData.parentClusterIndex];

        return new SpawnedRock(rockObj, rockData.gridPosition, rockData.layerIndex, parentCluster);
    }

    private SpawnedAnimal LoadAnimal(SpawnedAnimalData animalData, bool startHidden = false)
    {
        if (animalData.prefabIndex < 0 || animalData.prefabIndex >= PrefabConfig.Instance.animalPrefabs.Length)
        {
            Debug.LogWarning($"Invalid animal prefab index: {animalData.prefabIndex}");
            return null;
        }

        var animalPrefab = PrefabConfig.Instance.animalPrefabs[animalData.prefabIndex];
        var animalObj = PoolManager.Instance.Spawn(animalPrefab, animalData.currentPosition, Quaternion.identity);
        animalObj.transform.SetParent(transform);
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
            if (currentRegion != null && currentRegion.isActive) regionObj.OnRegionActivated();
        }

        if (animalObj.TryGetComponent(out Animal animalComponent))
        {
            animalComponent.layerIndex = animalData.layerIndex;
            
            if (animalComponent.health != null)
            {
                animalComponent.health.SetMaxHealth(1f, refillHealth: true);
                animalComponent.health.RestoreHealth();
            }

            var floorAgent = animalObj.GetComponentInChildren<FloorAgent>();
            if (floorAgent != null) floorAgent.MoveToFloor(animalData.layerIndex);
        }

        return new SpawnedAnimal(animalObj, Vector3Int.FloorToInt(animalData.currentPosition));
    }

    #endregion

    public int GetPrefabIndex(GameObject gameObj, GameObject[] prefabs)
    {
        var prefabName = gameObj.name.Replace("(Clone)", "");

        for (var i = 0; i < prefabs.Length; i++)
            if (prefabs[i].name == prefabName)
                return i;

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
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save data: {e.Message}");
        }
    }

    #endregion

    #endregion
}