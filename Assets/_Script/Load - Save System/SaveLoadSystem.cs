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

        unitManager = UnitManager.Instance;
        decorObjectParent = transform.Find("Decor Object");
    }

    private void Start()
    {
        saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();
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
        saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();

        foreach (var saveAble in saveables) saveAble.PopulateSaveData(saveData);

        var json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, json);

        Debug.Log($"Game saved to {saveFilePath}");
    }

    public void LoadGame()
    {
        if (loadAsync)
            return;

        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found!");
            return;
        }

        saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();
        
        var json = File.ReadAllText(saveFilePath);
        var saveData = JsonUtility.FromJson<GameSaveData>(json);

        foreach (var saveAble in saveables) saveAble.LoadFromSaveData(saveData);

        LoadTaskDataOnly(saveData);

        UnitManager.Instance.UpdateGraphNodeWhenStart();

        Debug.Log($"Game loaded from {saveFilePath}");
        
        OnLoaded?.Invoke();
    }

    public IEnumerator LoadGameAsync()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found!");
            yield break;
        }

        saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();

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
        
        foreach (var enemy in UnitManager.Instance.enemies)
        {
            if (enemy != null) enemy.GetBT()?.ClearState();
        }
        foreach (var unit in UnitManager.Instance.allUnits)
        {
            if (unit != null) unit.GetBT()?.ClearState();
        }

        Time.timeScale = originalTimeScale > 0 ? originalTimeScale : 1f;
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
        saveData.isWin = GameManager.Instance != null &&
                         GameManager.Instance.StateMachine.CurrentStateType == GameStateType.Win;
        saveData.isGameOver = GameManager.Instance != null &&
                              GameManager.Instance.StateMachine.CurrentStateType == GameStateType.GameOver;
        if (TimeOfDaySystem.Instance != null)
        {
            saveData.currentDay = TimeOfDaySystem.Instance.CurrentDay;
            saveData.currentTimeInDay = TimeOfDaySystem.Instance.GetCurrentTime();
        }

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
        
        #region Save Enemy Data
        saveData.enemySaveData.Clear();
        foreach (var enemy in unitManager.enemies)
        {
            if (enemy == null || enemy.health.CurrentHealth <= 0) continue;

            string matchedSpawnPointId = "";
            if (enemy.enemySpawnPoint != null)
            {
                if (enemy.enemySpawnPoint.TryGetComponent<SpawnPoint>(out var sp))
                {
                    matchedSpawnPointId = sp.GetId();
                }
            }

            saveData.enemySaveData.Add(new EnemySaveLoadData
            {
                id = enemy.GetId(),
                unitName = enemy.unitName,
                unitType = enemy.unitType,
                level = enemy.unitStatsManager.currentLevel,
                position = enemy.transform.position,
                layerIndex = enemy.floorAgent.currentFloorIndex,
                currentHealth = enemy.health.CurrentHealth,
                spawnPointId = matchedSpawnPointId 
            });
        }
        #endregion

        #region Save SpawnPoint Data
        saveData.spawnPointSaveData.Clear();
        var allSpawnPoints = FindObjectsOfType<SpawnPoint>();
        foreach (var sp in allSpawnPoints)
        {
            if (sp == null) continue;
            saveData.spawnPointSaveData.Add(new SpawnPointSaveLoadData
            {
                id = sp.GetId(),
                layerIndex = sp.layerIndex,
                position = sp.transform.position
            });
        }
        #endregion
        
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (WalletManager.Instance != null) WalletManager.Instance.SetCoinsOnLoad(saveData.totalCoins);

        if (TimeOfDaySystem.Instance != null)
        {
            TimeOfDaySystem.Instance.SetCurrentTime(saveData.currentTimeInDay);
            TimeOfDaySystem.Instance.SetCurrentDay(saveData.currentDay);

            #region Load Building & Storage Content

            var buildingData = saveData.buildingSaveData;

            foreach (var building in unitManager.buildings)
                PoolManager.Instance.Despawn(building.gameObject);
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

                building.customRenderer.layerIndex = building.LayerIndex;
            }

            #endregion

            #region Load Unit

            var unitData = saveData.unitSaveData;

            foreach (var unit in unitManager.allUnits)
                PoolManager.Instance.Despawn(unit.gameObject);
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
                                        guardComponent.listArcherPositions = new List<SpotData>();

                                    var isAlreadyRestored = guardComponent.listArcherPositions
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

            if (ItemManager.Instance != null) ItemManager.Instance.LoadItemsFromSaveData(saveData);

            #endregion

            #region Load Respawn Info

            if (ObjectSpawner.Instance != null) ObjectSpawner.Instance.LoadSpawnerFromSaveData(saveData);

            #endregion
            
            #region Load Enemy & SpawnPoint Links
            foreach (var enemy in unitManager.enemies.ToList())
            {
                if (enemy != null && enemy.gameObject != null)
                    PoolManager.Instance.Despawn(enemy.gameObject);
            }
            unitManager.enemies.Clear();
            
            if (SpawnManager.Instance != null && SpawnManager.Instance.spawnPoints != null)
            {
                SpawnManager.Instance.spawnPoints.Clear();
            }

            var oldSpawnPoints = FindObjectsOfType<SpawnPoint>();
            foreach (var sp in oldSpawnPoints)
            {
                if (sp != null && sp.gameObject != null)
                {
                    PoolManager.Instance.Despawn(sp.gameObject);
                }
            }

            var spawnPointLookup = new Dictionary<string, SpawnPoint>();
            GameObject spawnPointPrefab = PrefabConfig.Instance.spawnPointPrefab; 
            if (saveData.spawnPointSaveData != null && spawnPointPrefab != null)
            {
                foreach (var savedSP in saveData.spawnPointSaveData)
                {
                    GameObject spObj = PoolManager.Instance.Spawn(spawnPointPrefab, savedSP.position, Quaternion.identity);
                    if (spObj == null) continue;

                    SpawnPoint spComp = spObj.GetComponent<SpawnPoint>();
                    if (spComp != null)
                    {
                        if (spObj.TryGetComponent<UniqueId>(out var uniqueId))
                        {
                            uniqueId.OverrideId(savedSP.id);
                        }
                        
                        spComp.layerIndex = savedSP.layerIndex;

                        var spLayerName = $"Layer {savedSP.layerIndex + 1}";
                        spObj.layer = LayerMask.NameToLayer(spLayerName);

                        spawnPointLookup[savedSP.id] = spComp;
                        SpawnManager.Instance.spawnPoints.Add(spComp);
                        spComp.transform.SetParent(SpawnManager.Instance.transform);
                    }
                }
                Debug.Log($"[SaveLoadSystem] Đã khôi phục thành công {spawnPointLookup.Count} cổng sinh quái từ file save.");
            }

            if (saveData.enemySaveData != null)
            {
                foreach (var savedEnemy in saveData.enemySaveData)
                {

                    GameObject enemyPrefab = PrefabConfig.Instance.GetPrefab(savedEnemy.unitType.ToString());
                    if (enemyPrefab == null) continue;

                    GameObject enemyObj = PoolManager.Instance.Spawn(enemyPrefab, savedEnemy.position, Quaternion.identity);
                    if (enemyObj == null) continue;

                    Unit enemyComp = enemyObj.GetComponent<Unit>();
                    if (enemyComp != null)
                    {
                        enemyComp.OverrideId(savedEnemy.id);
                        enemyComp.unitType = savedEnemy.unitType;
                        enemyComp.unitName = savedEnemy.unitName;
                        enemyComp.unitStatsManager.SetLevel(savedEnemy.level);
                        enemyComp.characterMovement.CurrentLayer = savedEnemy.layerIndex;
                        enemyComp.floorAgent.MoveToFloor(savedEnemy.layerIndex);
                        
                        if (enemyComp.health != null && enemyComp.unitStatsManager != null)
                        {
                            enemyComp.health.maxHealth = enemyComp.unitStatsManager.MaxHealth;
                            enemyComp.health.SetCurrentHealth(savedEnemy.currentHealth);
                        }

                        enemyComp.currentState = UnitState.Idle;
                        enemyComp.animState = AnimState.Idle;

                        if (!string.IsNullOrEmpty(savedEnemy.spawnPointId) && spawnPointLookup.TryGetValue(savedEnemy.spawnPointId, out var sp))
                        {
                            enemyComp.enemySpawnPoint = sp.gameObject; 
                        }

                        unitManager.RegisterUnit(enemyComp);
                    }
                }
                Debug.Log($"[SaveLoadSystem] Đã hồi sinh xong {unitManager.enemies.Count} kẻ địch và gán về đúng cổng.");
            }

            #endregion
        }
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

        var regionTasks = new Dictionary<Vector2Int, List<DecorSpawnTask>>();

        void AddDecorTaskToRegion(Vector3 pos, DecorSpawnTask task)
        {
            if (RegionManager.Instance == null) return;

            var key = RegionManager.Instance.GetRegionKey(pos);
            if (!RegionManager.Instance.HasRegion(key)) return;

            if (!regionTasks.TryGetValue(key, out var list))
            {
                list = new List<DecorSpawnTask>();
                regionTasks[key] = list;
            }

            list.Add(task);
        }

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

            foreach (var data in layerData.trees)
            {
                var worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddDecorTaskToRegion(worldPos, new DecorSpawnTask(DecorType.Tree, currentLayerIndex, data));
            }

            foreach (var data in layerData.bushes)
            {
                var worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddDecorTaskToRegion(worldPos, new DecorSpawnTask(DecorType.Bush, currentLayerIndex, data));
            }

            foreach (var data in layerData.rocks)
            {
                var worldPos = ObjectSpawner.Instance.GridToWorld(data.gridPosition);
                AddDecorTaskToRegion(worldPos, new DecorSpawnTask(DecorType.Rock, currentLayerIndex, data));
            }

            foreach (var data in layerData.animals)
                AddDecorTaskToRegion(data.currentPosition,
                    new DecorSpawnTask(DecorType.Animal, currentLayerIndex, data));
        }

        var firstRegionKeys = RegionManager.Instance.GetRegionKeysAroundCamera();
        var cameraPos = Camera.main != null ? (Vector2)Camera.main.transform.position : Vector2.zero;

        var remainingRegionKeys = regionTasks.Keys
            .Where(key => !firstRegionKeys.Contains(key))
            .OrderBy(key => (RegionManager.Instance.GetRegionCenter(key) - cameraPos).sqrMagnitude)
            .ToList();

        var allSortedRegionKeys = new List<Vector2Int>();
        allSortedRegionKeys.AddRange(firstRegionKeys);
        allSortedRegionKeys.AddRange(remainingRegionKeys);

        var processedCount = 0;
        var clustersCache = ObjectSpawner.Instance.layerClusters;

        foreach (var key in allSortedRegionKeys)
        {
            if (!regionTasks.TryGetValue(key, out var tasks)) continue;

            var isBackground = !firstRegionKeys.Contains(key);

            foreach (var task in tasks)
            {
                switch (task.type)
                {
                    case DecorType.Tree:
                        var treeData = (SpawnedTreeData)task.dataReference;
                        var treeObj = LoadTree(treeData, clustersCache[task.layerIndex], isBackground);
                        if (treeObj != null) ObjectSpawner.Instance.spawnedTrees[task.layerIndex].Add(treeObj);
                        break;

                    case DecorType.Bush:
                        var bushData = (SpawnedBushData)task.dataReference;
                        var bushObj = LoadBush(bushData, clustersCache[task.layerIndex], isBackground);
                        if (bushObj != null) ObjectSpawner.Instance.spawnedBushes[task.layerIndex].Add(bushObj);
                        break;

                    case DecorType.Rock:
                        var rockData = (SpawnedRockData)task.dataReference;
                        var rockObj = LoadRock(rockData, clustersCache[task.layerIndex], isBackground);
                        if (rockObj != null) ObjectSpawner.Instance.spawnedRocks[task.layerIndex].Add(rockObj);
                        break;

                    case DecorType.Animal:
                        var animalData = (SpawnedAnimalData)task.dataReference;
                        var animalObj = LoadAnimal(animalData, isBackground);
                        if (animalObj != null) ObjectSpawner.Instance.spawnedAnimals[task.layerIndex].Add(animalObj);
                        break;
                }

                processedCount++;
                if (processedCount >= objectsPerFrame)
                {
                    processedCount = 0;
                    yield return null;
                }
            }
        }

        UnitManager.Instance.UpdateGraphNodeWhenStart();
        Debug.Log("[SaveLoadSystem] Đã khôi phục xong toàn bộ thực thể Decor trên bản đồ (Không sinh rác Action).");

        if (ObjectSpawner.Instance != null)
            ObjectSpawner.Instance.LinkChoppedTreesOnMapLoaded();

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

        var tree = treeObj.GetComponent<Tree>();
        if (tree != null && tree.spriteRenderer != null) tree.spriteRenderer.enabled = !startHidden;

        var anim = treeObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = !startHidden;

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
                treeComponent.customRender.gameObject.SetActive(false);
                //treeComponent.treeCollider.enabled = false;
            }

            if (treeComponent.customRender != null)
                treeComponent.customRender.layerIndex = treeData.layerIndex;

            GraphNode.Instance.SetWalkableNode(treeData.gridPosition, treeComponent.layerIndex, false);
        }

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
        bushObj.transform.SetParent(decorObjectParent);
        bushObj.OverrideId(bushData.id);

        // 1. Ép bật/tắt cẩn thận (Dọn sạch tàn dư của Object Pool)
        var bush = bushObj.GetComponent<Bush>();
        if (bush != null && bush.spriteRenderer != null) bush.spriteRenderer.enabled = !startHidden;

        var anim = bushObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = !startHidden;
        
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

        // 1. Ép bật/tắt cẩn thận (Dọn sạch tàn dư của Object Poo
        var rock = rockObj.GetComponent<Rock>();
        if (rock != null && rock.spriteRenderer != null) rock.spriteRenderer.enabled = !startHidden;

        var anim = rockObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = !startHidden;

        if (rockObj.TryGetComponent(out Rock rockComponent))
        {
            rockComponent.layerIndex = rockData.layerIndex;
            rockComponent.positionInGrid = rockData.gridPosition;

            //string layerName = $"Layer {rockData.layerIndex + 1}";
            var layerIndexMask = LayerMask.NameToLayer("Decor");
            rockObj.layer = layerIndexMask;
        }

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
        var animal = animalObj.GetComponent<Animal>();
        if (animal != null && animal.spriteRenderer != null) animal.spriteRenderer.enabled = !startHidden;

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
        if (!File.Exists(saveFilePath)) return false;

        try
        {
            var json = File.ReadAllText(saveFilePath);
            var temporaryData = JsonUtility.FromJson<GameSaveData>(json);

            if (temporaryData != null && (temporaryData.isWin || temporaryData.isGameOver))
            {
                if (temporaryData.isWin)
                    Debug.LogWarning(
                        "[SaveLoadSystem] 🏆 Phát hiện file save cũ đã CHIẾN THẮNG. Tiến hành dọn sạch Scene để lập map mới tinh từ MainMenu!");
                else
                    Debug.LogWarning(
                        "[SaveLoadSystem] 💀 Phát hiện file save cũ đã GAME OVER. Tiến hành dọn sạch Scene để lập map mới tinh từ MainMenu!");

                ClearCurrentSceneObjects(); 
                
                DeleteSaveData();
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadSystem] Lỗi rà soát file dữ liệu cũ: {e.Message}");
            return false;
        }

        return true;
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

    public void ClearCurrentSceneObjects()
    {
        if (UnitManager.Instance == null) return;

        var activeBuildings = UnitManager.Instance.buildings.ToList();
        foreach (var building in activeBuildings)
            if (building != null && building.gameObject != null)
                PoolManager.Instance.Despawn(building.gameObject);

        UnitManager.Instance.buildings.Clear();

        var activeUnits = UnitManager.Instance.allUnits.ToList();
        foreach (var unit in activeUnits)
            if (unit != null && unit.gameObject != null)
                PoolManager.Instance.Despawn(unit.gameObject);

        UnitManager.Instance.allUnits.Clear();

        if (ItemManager.Instance != null)
        {
            var activeItemsOnGround = ItemManager.Instance.activeItems.ToList();
            foreach (var item in activeItemsOnGround)
            {
                if (item != null && item.gameObject != null) PoolManager.Instance.Despawn(item.gameObject);
            }
            ItemManager.Instance.activeItems.Clear();
            ItemManager.Instance.ReleasePendingItems();
            
            var remainingItems = ItemManager.Instance.activeItems.ToList();
            foreach (var item in remainingItems)
            {
                if (item != null && item.gameObject != null) PoolManager.Instance.Despawn(item.gameObject);
            }
            ItemManager.Instance.activeItems.Clear();

            var remainingCoinsOnGround = ItemManager.Instance.activeCoins.ToList();
            foreach (var coin in remainingCoinsOnGround)
                if (coin != null && coin.gameObject != null)
                    PoolManager.Instance.Despawn(coin.gameObject);
            ItemManager.Instance.activeCoins.Clear();
        }

        if (ObjectSpawner.Instance != null)
        {
            foreach (var layer in ObjectSpawner.Instance.spawnedTrees.Values)
            foreach (var tree in layer)
                if (tree.treeComponent != null)
                    PoolManager.Instance.Despawn(tree.treeComponent.gameObject);

            foreach (var layer in ObjectSpawner.Instance.spawnedBushes.Values)
            foreach (var bush in layer)
                if (bush.bushObject != null)
                    PoolManager.Instance.Despawn(bush.bushObject);

            foreach (var layer in ObjectSpawner.Instance.spawnedRocks.Values)
            foreach (var rock in layer)
                if (rock.rockObject != null)
                    PoolManager.Instance.Despawn(rock.rockObject);

            foreach (var layer in ObjectSpawner.Instance.spawnedAnimals.Values)
            foreach (var animal in layer)
                if (animal.animalObject != null)
                    PoolManager.Instance.Despawn(animal.animalObject);

            ObjectSpawner.Instance.spawnedTrees.Clear();
            ObjectSpawner.Instance.spawnedBushes.Clear();
            ObjectSpawner.Instance.spawnedRocks.Clear();
            ObjectSpawner.Instance.spawnedAnimals.Clear();

            if (ObjectSpawner.Instance.layerClusters != null) ObjectSpawner.Instance.layerClusters.Clear();
        }
        
        var remainingEnemies = UnitManager.Instance.enemies.ToList();
        foreach (var enemy in remainingEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
                PoolManager.Instance.Despawn(enemy.gameObject);
        }
        UnitManager.Instance.enemies.Clear();

        if (EditBuildingManager.Instance != null) EditBuildingManager.Instance.ResetEditorManager();
    }

    #endregion

    #endregion
}