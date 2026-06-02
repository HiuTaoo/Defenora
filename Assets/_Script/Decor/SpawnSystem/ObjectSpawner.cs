using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Object_Pooling;
using UnityEngine;
using Random = System.Random;

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner Instance;

    [SerializeField] public ObjectSpawnSettings spawnSettings;

    public Dictionary<int, List<TreeCluster>> layerClusters = new Dictionary<int, List<TreeCluster>>();
    public Dictionary<int, List<SpawnedTree>> spawnedTrees = new Dictionary<int, List<SpawnedTree>>();
    public Dictionary<int, List<SpawnedBush>> spawnedBushes = new Dictionary<int, List<SpawnedBush>>();
    public Dictionary<int, List<SpawnedRock>> spawnedRocks = new Dictionary<int, List<SpawnedRock>>();
    public Dictionary<int, List<SpawnedAnimal>> spawnedAnimals = new Dictionary<int, List<SpawnedAnimal>>();

    public Dictionary<int, HashSet<Vector3Int>> occupiedPositions = new Dictionary<int, HashSet<Vector3Int>>();

    [Header("--- Respawn Queue System ---")]
    [Tooltip("Giờ trong game sẽ thực hiện làm sạch và hồi phục tài nguyên (Ví dụ: 3 = 3h sáng)")]
    [SerializeField]
    private int respawnHour = 3;

    [SerializeField] private List<Tree> choppedTreeQueue = new();
    [SerializeField] private List<int> harvestedBushLayerQueue = new();
    [SerializeField] private List<int> deadAnimalLayerQueue = new();
    private List<ChoppedTreeSaveEntry> _tempChoppedTreeData = new();

    private int _bushDayTracker;
    private Random random;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            random = new Random();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (TimeOfDaySystem.Instance != null) TimeOfDaySystem.Instance.OnHourChanged += HandleHourChangedBroadcast;
    }

    private void OnDisable()
    {
        if (TimeOfDaySystem.Instance != null) TimeOfDaySystem.Instance.OnHourChanged -= HandleHourChangedBroadcast;
    }

    #region Spawn

    #region Spawn Objects

    private IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(0.1f);
        SpawnObjectsOnAllLayers();
    }

    public void SpawnObjectsOnAllLayers()
    {
        foreach (var layerGraph in GraphNode.Instance.layerGraphs)
        {
            SpawnObjectsOnLayer(layerGraph.Key);
        }
    }

    public void SpawnObjectsOnLayer(int layerIndex)
    {
        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
        {
            Debug.LogWarning($"Không tìm thấy graph cho layer {layerIndex}");
            return;
        }

        if (!occupiedPositions.ContainsKey(layerIndex))
        {
            occupiedPositions[layerIndex] = new HashSet<Vector3Int>();
        }
        else
        {
            occupiedPositions[layerIndex].Clear();
        }

        //Debug.Log($"Bắt đầu spawn cây cho layer {layerIndex}");

        List<Vector3Int> potentialSpawnPoints = GetPotentialSpawnPoints(graph);

        List<TreeCluster> clusters = CreateNoiseClusters(potentialSpawnPoints, layerIndex);

        List<SpawnedTree> trees = SpawnTreesInClusters(clusters);

        foreach (var tree in trees)
        {
            occupiedPositions[layerIndex].Add(tree.gridPosition);
        }

        List<SpawnedBush> bushes = SpawnBushesOnLayer(layerIndex, trees, clusters);

        foreach (var bush in bushes)
        {
            occupiedPositions[layerIndex].Add(bush.gridPosition);
        }

        List<SpawnedRock> rocks = SpawnRocksOnLayer(layerIndex, trees, clusters);

        foreach (var rock in rocks)
        {
            occupiedPositions[layerIndex].Add(rock.gridPosition);
        }

        List<SpawnedAnimal> animals = SpawnAnimalsOnLayer(layerIndex, trees, bushes, rocks, clusters);

        layerClusters[layerIndex] = clusters;
        spawnedTrees[layerIndex] = trees;
        spawnedBushes[layerIndex] = bushes;
        spawnedRocks[layerIndex] = rocks;
        spawnedAnimals[layerIndex] = animals;

        //Debug.Log($"Đã spawn {trees.Count} cây và {bushes.Count} bụi cỏ trong {clusters.Count} clusters cho layer {layerIndex}");
    }

    #endregion

    #region Spawn Tree
    private List<Vector3Int> GetPotentialSpawnPoints(PathfindingGraph graph)
    {
        List<Vector3Int> spawnPoints = new List<Vector3Int>();

        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;

            if (!node.isWalkable) continue;

            if (node.isBridge) continue;

            if (!node.isWalkable) continue;

            if (spawnSettings.avoidStairs && node.isStair) continue;

            if (spawnSettings.avoidStairs && IsNearStairs(node, graph)) continue;

            if(spawnSettings.avoidBridge && IsNearBridges(node, graph)) continue;

            float noiseValue = GetNoiseValue(node.position);

            if (noiseValue > spawnSettings.noiseThreshold)
            {
                spawnPoints.Add(node.position);
            }
        }

        return spawnPoints;
    }

    private bool IsNearStairs(Node node, PathfindingGraph graph)
    {
        float avoidanceRadius = spawnSettings.stairAvoidanceRadius;

        foreach (var kvp in graph.nodes)
        {
            Node otherNode = kvp.Value;
            if (otherNode.isStair)
            {
                float distance = Vector3Int.Distance(node.position, otherNode.position);
                if (distance <= avoidanceRadius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsNearBridges(Node node, PathfindingGraph graph)
    {
        float avoidanceRadius = spawnSettings.stairAvoidanceRadius;

        foreach (var kvp in graph.nodes)
        {
            Node otherNode = kvp.Value;
            if (otherNode.isBridge)
            {
                float distance = Vector3Int.Distance(node.position, otherNode.position);
                if (distance <= avoidanceRadius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private float GetNoiseValue(Vector3Int position)
    {
        float x = (position.x + spawnSettings.noiseOffset.x) * spawnSettings.noiseScale;
        float y = (position.y + spawnSettings.noiseOffset.y) * spawnSettings.noiseScale;

        return Mathf.PerlinNoise(x, y);
    }

    private List<TreeCluster> CreateNoiseClusters(List<Vector3Int> spawnPoints, int layerIndex)
    {
        List<TreeCluster> clusters = new List<TreeCluster>();
        List<Vector3Int> unprocessedPoints = new List<Vector3Int>(spawnPoints);

        while (unprocessedPoints.Count > 0)
        {
            Vector3Int centerPoint = GetHighestNoisePoint(unprocessedPoints);
            float centerNoise = GetNoiseValue(centerPoint);

            TreeCluster cluster = new TreeCluster(centerPoint, layerIndex, centerNoise);

            List<Vector3Int> clusterPoints = GetPointsInRadius(centerPoint, unprocessedPoints, spawnSettings.clusterRadius);

            if (clusterPoints.Count > spawnSettings.maxClusterSize)
            {
                clusterPoints = clusterPoints.Take(spawnSettings.maxClusterSize).ToList();
            }

            if (clusterPoints.Count >= spawnSettings.minClusterSize)
            {
                cluster.nodePositions = clusterPoints;
                cluster.desiredTreeCount = Mathf.RoundToInt(clusterPoints.Count * spawnSettings.spawnDensity);
                clusters.Add(cluster);
            }

            foreach (var point in clusterPoints)
            {
                unprocessedPoints.Remove(point);
            }

            if (clusterPoints.Count < spawnSettings.minClusterSize)
            {
                unprocessedPoints.Remove(centerPoint);
            }
        }
        return clusters;
    }
    private Vector3Int GetHighestNoisePoint(List<Vector3Int> points)
    {
        Vector3Int bestPoint = points[0];
        float bestNoise = GetNoiseValue(bestPoint);

        foreach (var point in points)
        {
            float noise = GetNoiseValue(point);
            if (noise > bestNoise)
            {
                bestNoise = noise;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    private List<Vector3Int> GetPointsInRadius(Vector3Int center, List<Vector3Int> points, float radius)
    {
        List<Vector3Int> result = new List<Vector3Int>();

        foreach (var point in points)
        {
            float distance = Vector3Int.Distance(center, point);
            if (distance <= radius)
            {
                result.Add(point);
            }
        }

        return result;
    }

    private List<SpawnedTree> SpawnTreesInClusters(List<TreeCluster> clusters)
    {
        List<SpawnedTree> allTrees = new List<SpawnedTree>();

        foreach (var cluster in clusters)
        {
            List<SpawnedTree> clusterTrees = SpawnTreesInCluster(cluster);
            allTrees.AddRange(clusterTrees);
        }

        return allTrees;
    }

    private List<SpawnedTree> SpawnTreesInCluster(TreeCluster cluster)
    {
        List<SpawnedTree> trees = new List<SpawnedTree>();

        List<Vector3Int> shuffledPositions = new List<Vector3Int>(cluster.nodePositions);
        for (int i = 0; i < shuffledPositions.Count; i++)
        {
            Vector3Int temp = shuffledPositions[i];
            int randomIndex = random.Next(i, shuffledPositions.Count);
            shuffledPositions[i] = shuffledPositions[randomIndex];
            shuffledPositions[randomIndex] = temp;
        }

        int treesSpawned = 0;
        for (int i = 0; i < shuffledPositions.Count && treesSpawned < cluster.desiredTreeCount; i++)
        {
            Vector3Int position = shuffledPositions[i];

            if (IsTooCloseToExistingTree(position, trees))
                continue;

            // Check if position is already occupied by any object
            if (IsPositionOccupied(position, cluster.layerIndex))
                continue;

            GameObject treePrefab = PrefabConfig.Instance.treePrefabs[random.Next(PrefabConfig.Instance.treePrefabs.Length)];
            Vector3 worldPosition = GridToWorld(position);

            var treeObj = PoolManager.Instance.Spawn(treePrefab, worldPosition, Quaternion.identity);

            if (treeObj.TryGetComponent<Tree>(out Tree treeComponent))
            {
                treeComponent.layerIndex = cluster.layerIndex;

                string layerName = $"Layer {cluster.layerIndex + 1}";
                int layerIndex = LayerMask.NameToLayer(layerName);
                treeObj.layer = layerIndex;

                treeComponent.positionInGrid = position;
                GraphNode.Instance.SetWalkableNode(position, treeComponent.layerIndex, false);
            }

            var spriteRenderer = treeObj.GetComponent<SpriteRenderer>();

            var renderObj = treeObj.transform.Find("Custom Render Sprite");
            var renderComponent = renderObj.GetComponent<CustomRender>();
            if (renderComponent != null)
                renderComponent.layerIndex = cluster.layerIndex;

            SpawnedTree spawnedTree = new SpawnedTree(treeComponent, position, cluster.layerIndex, cluster);
            trees.Add(spawnedTree);
            treesSpawned++;
        }

        return trees;
    }
    #endregion

    #region Spawn Bush
    private bool IsTooCloseToExistingTree(Vector3Int position, List<SpawnedTree> existingTrees)
    {
        foreach (var tree in existingTrees)
        {
            float distance = Vector3Int.Distance(position, tree.gridPosition);
            if (distance < spawnSettings.treeSpacing)
            {
                return true;
            }
        }
        return false;
    }

    private List<SpawnedBush> SpawnBushesOnLayer(int layerIndex, List<SpawnedTree> trees, List<TreeCluster> clusters)
    {
        if (PrefabConfig.Instance.bushPrefabs == null || PrefabConfig.Instance.bushPrefabs.Length == 0)
        {
            return new List<SpawnedBush>();
        }

        List<SpawnedBush> bushes = new List<SpawnedBush>();

        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
        {
            return bushes;
        }

        foreach (var cluster in clusters)
        {
            List<SpawnedBush> clusterBushes = SpawnBushesInCluster(cluster, trees, graph, layerIndex);
            bushes.AddRange(clusterBushes);
        }

        List<SpawnedBush> scatteredBushes = SpawnScatteredBushes(layerIndex, trees, clusters, graph);
        bushes.AddRange(scatteredBushes);

        return bushes;
    }

    private List<SpawnedBush> SpawnBushesInCluster(TreeCluster cluster, List<SpawnedTree> trees, PathfindingGraph graph, int layerIndex)
    {
        List<SpawnedBush> bushes = new List<SpawnedBush>();

        List<SpawnedTree> clusterTrees = trees.Where(t => t.parentCluster == cluster).ToList();

        foreach (var tree in clusterTrees)
        {
            List<Vector3Int> bushPositions = GetBushPositionsAroundTree(tree.gridPosition, graph);

            foreach (var bushPos in bushPositions)
            {
                if (IsPositionOccupied(bushPos, layerIndex))
                    continue;

                if (IsTooCloseToTree(bushPos, trees) || IsTooCloseToBush(bushPos, bushes))
                    continue;

                if (random.NextDouble() < spawnSettings.bushSpawnChance)
                {
                    SpawnedBush bush = SpawnBushAtPosition(bushPos, cluster.layerIndex, cluster);
                    if (bush != null)
                    {
                        bushes.Add(bush);
                        occupiedPositions[layerIndex].Add(bush.gridPosition);
                    }
                }
            }
        }

        return bushes;
    }

    private List<SpawnedBush> SpawnScatteredBushes(int layerIndex, List<SpawnedTree> trees, List<TreeCluster> clusters, PathfindingGraph graph)
    {
        List<SpawnedBush> bushes = new List<SpawnedBush>();

        List<Vector3Int> potentialBushPositions = new List<Vector3Int>();
        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            if (!node.isWalkable || node.isBridge || node.isStair) continue;

            if (IsPositionOccupied(node.position, layerIndex)) continue;

            if (spawnSettings.avoidStairs && IsNearStairs(node, graph)) continue;

            if (IsTooCloseToTree(node.position, trees)) continue;

            float bushNoise = GetBushNoiseValue(node.position);
            if (bushNoise > spawnSettings.bushNoiseThreshold)
            {
                potentialBushPositions.Add(node.position);
            }
        }

        foreach (var pos in potentialBushPositions)
        {
            if (random.NextDouble() < spawnSettings.scatteredBushSpawnChance)
            {
                if (!IsPositionOccupied(pos, layerIndex) && !IsTooCloseToBush(pos, bushes))
                {
                    SpawnedBush bush = SpawnBushAtPosition(pos, layerIndex, null);
                    if (bush != null)
                    {
                        bushes.Add(bush);
                    }
                }
            }
        }

        return bushes;
    }

    private List<Vector3Int> GetBushPositionsAroundTree(Vector3Int treePosition, PathfindingGraph graph)
    {
        List<Vector3Int> positions = new List<Vector3Int>();

        for (int x = -spawnSettings.bushAroundTreeRadius; x <= spawnSettings.bushAroundTreeRadius; x++)
        {
            for (int y = -spawnSettings.bushAroundTreeRadius; y <= spawnSettings.bushAroundTreeRadius; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector3Int checkPos = treePosition + new Vector3Int(x, y, 0);

                if (graph.nodes.TryGetValue(checkPos, out Node node))
                {
                    if (node.isWalkable && !node.isBridge && !node.isStair)
                    {
                        float distance = Vector3Int.Distance(treePosition, checkPos);
                        if (distance <= spawnSettings.bushAroundTreeRadius)
                        {
                            positions.Add(checkPos);
                        }
                    }
                }
            }
        }

        return positions;
    }

    private float GetBushNoiseValue(Vector3Int position)
    {
        float x = (position.x + spawnSettings.bushNoiseOffset.x) * spawnSettings.bushNoiseScale;
        float y = (position.y + spawnSettings.bushNoiseOffset.y) * spawnSettings.bushNoiseScale;

        return Mathf.PerlinNoise(x, y);
    }

    private bool IsTooCloseToTree(Vector3Int position, List<SpawnedTree> trees)
    {
        foreach (var tree in trees)
        {
            float distance = Vector3Int.Distance(position, tree.gridPosition);
            if (distance < spawnSettings.bushToTreeMinDistance)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsTooCloseToBush(Vector3Int position, List<SpawnedBush> bushes)
    {
        foreach (var bush in bushes)
        {
            float distance = Vector3Int.Distance(position, bush.gridPosition);
            if (distance < spawnSettings.bushSpacing)
            {
                return true;
            }
        }
        return false;
    }

    private SpawnedBush SpawnBushAtPosition(Vector3Int position, int layerIndex, TreeCluster parentCluster)
    {
        GameObject bushPrefab = PrefabConfig.Instance.bushPrefabs[random.Next(PrefabConfig.Instance.bushPrefabs.Length)];
        Vector3 worldPosition = GridToWorld(position);

        var bushObj = PoolManager.Instance.Spawn(bushPrefab, worldPosition, Quaternion.identity);

        if (bushObj.TryGetComponent<Bush>(out Bush bushComponent))
        {
            bushComponent.layerIndex = layerIndex;

            //string layerName = $"Layer {layerIndex + 1}";
            int layerIndexMask = LayerMask.NameToLayer("Decor");
            bushObj.layer = layerIndexMask;

            bushComponent.positionInGrid = position;

        }
        return new SpawnedBush(bushObj, position, layerIndex, parentCluster);
    }
    #endregion

    #region Spawn Rock
    private List<SpawnedRock> SpawnRocksOnLayer(int layerIndex, List<SpawnedTree> trees, List<TreeCluster> clusters)
    {
        if (PrefabConfig.Instance.rockPrefabs == null || PrefabConfig.Instance.rockPrefabs.Length == 0)
        {
            return new List<SpawnedRock>();
        }

        List<SpawnedRock> rocks = new List<SpawnedRock>();

        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
        {
            return rocks;
        }

        // Spawn rocks in clusters (near trees)
        foreach (var cluster in clusters)
        {
            List<SpawnedRock> clusterRocks = SpawnRocksInCluster(cluster, trees, graph, layerIndex);
            rocks.AddRange(clusterRocks);
        }

        // Spawn scattered rocks
        List<SpawnedRock> scatteredRocks = SpawnScatteredRocks(layerIndex, trees, clusters, graph);
        rocks.AddRange(scatteredRocks);

        return rocks;
    }

    private List<SpawnedRock> SpawnRocksInCluster(TreeCluster cluster, List<SpawnedTree> trees, PathfindingGraph graph, int layerIndex)
    {
        List<SpawnedRock> rocks = new List<SpawnedRock>();

        List<SpawnedTree> clusterTrees = trees.Where(t => t.parentCluster == cluster).ToList();

        foreach (var tree in clusterTrees)
        {
            if (random.NextDouble() < spawnSettings.rockSpawnChance)
            {
                List<Vector3Int> rockPositions = GetRockPositionsAroundTree(tree.gridPosition, graph);

                foreach (var rockPos in rockPositions)
                {
                    // Check if position is already occupied by any object
                    if (IsPositionOccupied(rockPos, layerIndex))
                        continue;

                    if (IsTooCloseToTree(rockPos, trees) || IsTooCloseToRock(rockPos, rocks))
                        continue;

                    if (random.NextDouble() < spawnSettings.rockSpawnChance * 0.5f)
                    {
                        SpawnedRock rock = SpawnRockAtPosition(rockPos, cluster.layerIndex, cluster);
                        if (rock != null)
                        {
                            rocks.Add(rock);
                            occupiedPositions[layerIndex].Add(rockPos);
                        }
                    }
                }
            }
        }

        return rocks;
    }

    private List<SpawnedRock> SpawnScatteredRocks(int layerIndex, List<SpawnedTree> trees, List<TreeCluster> clusters, PathfindingGraph graph)
    {
        List<SpawnedRock> rocks = new List<SpawnedRock>();

        List<Vector3Int> potentialRockPositions = new List<Vector3Int>();
        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            if (!node.isWalkable || node.isBridge || node.isStair) continue;

            // Check if position is already occupied by any object
            if (IsPositionOccupied(node.position, layerIndex)) continue;

            if (spawnSettings.avoidStairs && IsNearStairs(node, graph)) continue;

            if (IsTooCloseToTree(node.position, trees)) continue;

            float rockNoise = GetRockNoiseValue(node.position);
            if (rockNoise > spawnSettings.rockNoiseThreshold)
            {
                potentialRockPositions.Add(node.position);
            }
        }

        foreach (var pos in potentialRockPositions)
        {
            if (random.NextDouble() < spawnSettings.scatteredRockSpawnChance)
            {
                // Double check if position is still available
                if (!IsPositionOccupied(pos, layerIndex))
                {
                    SpawnedRock rock = SpawnRockAtPosition(pos, layerIndex, null);
                    if (rock != null)
                    {
                        rocks.Add(rock);
                    }
                }
            }
        }

        return rocks;
    }

    private List<Vector3Int> GetRockPositionsAroundTree(Vector3Int treePosition, PathfindingGraph graph)
    {
        List<Vector3Int> positions = new List<Vector3Int>();

        for (int x = -spawnSettings.rockAroundTreeRadius; x <= spawnSettings.rockAroundTreeRadius; x++)
        {
            for (int y = -spawnSettings.rockAroundTreeRadius; y <= spawnSettings.rockAroundTreeRadius; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector3Int checkPos = treePosition + new Vector3Int(x, y, 0);

                if (graph.nodes.TryGetValue(checkPos, out Node node))
                {
                    if (node.isWalkable && !node.isBridge && !node.isStair)
                    {
                        float distance = Vector3Int.Distance(treePosition, checkPos);
                        if (distance <= spawnSettings.rockAroundTreeRadius)
                        {
                            positions.Add(checkPos);
                        }
                    }
                }
            }
        }

        return positions;
    }

    private float GetRockNoiseValue(Vector3Int position)
    {
        float x = (position.x + spawnSettings.rockNoiseOffset.x) * spawnSettings.rockNoiseScale;
        float y = (position.y + spawnSettings.rockNoiseOffset.y) * spawnSettings.rockNoiseScale;

        return Mathf.PerlinNoise(x, y);
    }

    private bool IsTooCloseToRock(Vector3Int position, List<SpawnedRock> rocks)
    {
        foreach (var rock in rocks)
        {
            float distance = Vector3Int.Distance(position, rock.gridPosition);
            if (distance < spawnSettings.rockSpacing)
            {
                return true;
            }
        }
        return false;
    }

    private SpawnedRock SpawnRockAtPosition(Vector3Int position, int layerIndex, TreeCluster parentCluster)
    {
        GameObject rockPrefab = PrefabConfig.Instance.rockPrefabs[random.Next(PrefabConfig.Instance.rockPrefabs.Length)];
        Vector3 worldPosition = GridToWorld(position);

        var rockObj = PoolManager.Instance.Spawn(rockPrefab, worldPosition, Quaternion.identity);

        if (rockObj.TryGetComponent<Rock>(out Rock rockComponent))
        {
            rockComponent.layerIndex = layerIndex;

            //string layerName = $"Layer {layerIndex + 1}";
            int layerIndexMask = LayerMask.NameToLayer("Decor");
            rockObj.layer = layerIndexMask;

            rockComponent.positionInGrid = position;

            if (spawnSettings.rocksBlockMovement)
            {
                GraphNode.Instance.SetWalkableNode(position, layerIndex, false);
            }
        }

        return new SpawnedRock(rockObj, position, layerIndex, parentCluster);
    }
    #endregion

    #region Spawn Animal
    private List<SpawnedAnimal> SpawnAnimalsOnLayer(int layerIndex, List<SpawnedTree> trees, List<SpawnedBush> bushes, List<SpawnedRock> rocks, List<TreeCluster> clusters)
    {
        if (PrefabConfig.Instance.animalPrefabs == null || PrefabConfig.Instance.animalPrefabs.Length == 0)
        {
            return new List<SpawnedAnimal>();
        }

        List<SpawnedAnimal> animals = new List<SpawnedAnimal>();

        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
        {
            return animals;
        }

        List<Vector3Int> potentialAnimalPositions = GetPotentialAnimalPositions(layerIndex, trees, bushes, rocks, clusters, graph);

        foreach (var pos in potentialAnimalPositions)
        {
            if (random.NextDouble() < spawnSettings.animalSpawnChance)
            {
                SpawnedAnimal animal = SpawnAnimalAtPosition(pos, layerIndex);
                if (animal != null)
                {
                    animals.Add(animal);
                }
            }
        }

        return animals;
    }

    private List<Vector3Int> GetPotentialAnimalPositions(int layerIndex, List<SpawnedTree> trees, List<SpawnedBush> bushes, List<SpawnedRock> rocks, List<TreeCluster> clusters, PathfindingGraph graph)
    {
        List<Vector3Int> positions = new List<Vector3Int>();

        HashSet<Vector3Int> occupiedPositions = new HashSet<Vector3Int>();

        foreach (var tree in trees) occupiedPositions.Add(tree.gridPosition);
        foreach (var bush in bushes) occupiedPositions.Add(bush.gridPosition);
        foreach (var rock in rocks) occupiedPositions.Add(rock.gridPosition);

        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            if (!node.isWalkable || node.isBridge || node.isStair) continue;

            if (occupiedPositions.Contains(node.position)) continue;

            if (spawnSettings.avoidStairs && IsNearStairs(node, graph)) continue;

            if (HasMinimumClearance(node.position, trees, bushes, rocks))
            {
                positions.Add(node.position);
            }
        }

        return positions;
    }

    private bool HasMinimumClearance(Vector3Int position, List<SpawnedTree> trees, List<SpawnedBush> bushes, List<SpawnedRock> rocks)
    {
        foreach (var tree in trees)
        {
            float distance = Vector3Int.Distance(position, tree.gridPosition);
            if (distance < spawnSettings.animalMinDistanceFromObstacles)
            {
                return false;
            }
        }

        foreach (var rock in rocks)
        {
            float distance = Vector3Int.Distance(position, rock.gridPosition);
            if (distance < spawnSettings.animalMinDistanceFromObstacles)
            {
                return false;
            }
        }

        return true;
    }

    private SpawnedAnimal SpawnAnimalAtPosition(Vector3Int position, int layerIndex)
    {
        GameObject animalPrefab = PrefabConfig.Instance.animalPrefabs[random.Next(PrefabConfig.Instance.animalPrefabs.Length)];
        Vector3 worldPosition = GridToWorld(position);

        var animalObj = PoolManager.Instance.Spawn(animalPrefab, worldPosition, Quaternion.identity);

        if (animalObj.TryGetComponent<Animal>(out Animal animalComponent))
        {
            animalComponent.layerIndex = layerIndex;

            if (animalComponent.floorAgent != null)
            {
                animalComponent.floorAgent.MoveToFloor(layerIndex);
            }
        }
        return new SpawnedAnimal(animalObj, position);
    }
    #endregion

    #endregion

    #region Respawn System

    /// <summary>
    /// Respawn trees trên toàn bộ map cho layer chỉ định
    /// </summary>
    public void RespawnTreesOnLayer(int layerIndex, int maxRespawnCount = 5)
    {
        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
            return;

        List<Vector3Int> validPositions = GetValidTreeRespawnPositions(layerIndex, graph);
        if (validPositions.Count == 0) return;

        List<SpawnedTree> currentTrees = GetTreesOnLayer(layerIndex);
        int respawnCount = 0;

        for (int i = 0; i < validPositions.Count; i++)
        {
            Vector3Int temp = validPositions[i];
            int randomIndex = random.Next(i, validPositions.Count);
            validPositions[i] = validPositions[randomIndex];
            validPositions[randomIndex] = temp;
        }

        foreach (var position in validPositions)
        {
            if (respawnCount >= maxRespawnCount) break;

            if (IsTooCloseToExistingTree(position, currentTrees)) continue;

            TreeCluster suitableCluster = FindSuitableCluster(position, layerIndex);
            if (suitableCluster == null)
            {
                float noiseValue = GetNoiseValue(position);
                suitableCluster = new TreeCluster(position, layerIndex, noiseValue);
                suitableCluster.nodePositions = new List<Vector3Int> { position };
                suitableCluster.desiredTreeCount = 1;

                if (!layerClusters.ContainsKey(layerIndex))
                    layerClusters[layerIndex] = new List<TreeCluster>();
                layerClusters[layerIndex].Add(suitableCluster);
            }

            SpawnedTree newTree = SpawnTreeAtPosition(position, layerIndex, suitableCluster);
            if (newTree != null)
            {
                if (!spawnedTrees.ContainsKey(layerIndex))
                    spawnedTrees[layerIndex] = new List<SpawnedTree>();
                spawnedTrees[layerIndex].Add(newTree);
                currentTrees.Add(newTree); 
                respawnCount++;
            }
        }

        Debug.Log($"Respawned {respawnCount} trees on layer {layerIndex}");
    }

    /// <summary>
    /// Respawn bushes trên toàn bộ map cho layer chỉ định
    /// </summary>
    public void RespawnBushesOnLayer(int layerIndex, int maxRespawnCount = 10)
    {
        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
            return;

        List<Vector3Int> validPositions = GetValidBushRespawnPositions(layerIndex, graph);
        if (validPositions.Count == 0) return;

        List<SpawnedTree> trees = GetTreesOnLayer(layerIndex);
        List<SpawnedBush> currentBushes = GetBushesOnLayer(layerIndex);
        int respawnCount = 0;

        for (int i = 0; i < validPositions.Count; i++)
        {
            Vector3Int temp = validPositions[i];
            int randomIndex = random.Next(i, validPositions.Count);
            validPositions[i] = validPositions[randomIndex];
            validPositions[randomIndex] = temp;
        }

        foreach (var position in validPositions)
        {
            if (respawnCount >= maxRespawnCount) break;

            if (IsTooCloseToTree(position, trees) || IsTooCloseToBush(position, currentBushes))
                continue;

            if (random.NextDouble() < spawnSettings.bushSpawnChance)
            {
                SpawnedBush newBush = SpawnBushAtPosition(position, layerIndex, null);
                if (newBush != null)
                {
                    if (!spawnedBushes.ContainsKey(layerIndex))
                        spawnedBushes[layerIndex] = new List<SpawnedBush>();
                    spawnedBushes[layerIndex].Add(newBush);
                    currentBushes.Add(newBush); // Update current list
                    respawnCount++;
                }
            }
        }

        Debug.Log($"Respawned {respawnCount} bushes on layer {layerIndex}");
    }

    /// <summary>
    /// Respawn rocks trên toàn bộ map cho layer chỉ định
    /// </summary>
    public void RespawnRocksOnLayer(int layerIndex, int maxRespawnCount = 8)
    {
        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
            return;

        List<Vector3Int> validPositions = GetValidRockRespawnPositions(layerIndex, graph);
        if (validPositions.Count == 0) return;

        List<SpawnedTree> trees = GetTreesOnLayer(layerIndex);
        List<SpawnedRock> currentRocks = GetRocksOnLayer(layerIndex);
        int respawnCount = 0;

        // Shuffle positions
        for (int i = 0; i < validPositions.Count; i++)
        {
            Vector3Int temp = validPositions[i];
            int randomIndex = random.Next(i, validPositions.Count);
            validPositions[i] = validPositions[randomIndex];
            validPositions[randomIndex] = temp;
        }

        foreach (var position in validPositions)
        {
            if (respawnCount >= maxRespawnCount) break;

            if (IsTooCloseToTree(position, trees) || IsTooCloseToRock(position, currentRocks))
                continue;

            if (random.NextDouble() < spawnSettings.rockSpawnChance)
            {
                SpawnedRock newRock = SpawnRockAtPosition(position, layerIndex, null);
                if (newRock != null)
                {
                    if (!spawnedRocks.ContainsKey(layerIndex))
                        spawnedRocks[layerIndex] = new List<SpawnedRock>();
                    spawnedRocks[layerIndex].Add(newRock);
                    currentRocks.Add(newRock); 
                    respawnCount++;
                }
            }
        }

        Debug.Log($"Respawned {respawnCount} rocks on layer {layerIndex}");
    }

    /// <summary>
    /// Respawn animals trên toàn bộ map cho layer chỉ định
    /// </summary>
    public void RespawnAnimalsOnLayer(int layerIndex, int maxRespawnCount = 10)
    {
        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
            return;

        List<Vector3Int> validPositions = GetValidAnimalRespawnPositions(layerIndex, graph);
        if (validPositions.Count == 0) return;

        List<SpawnedAnimal> currentAnimals = GetAnimalsOnLayer(layerIndex);
        int respawnCount = 0;

        // Shuffle positions
        for (int i = 0; i < validPositions.Count; i++)
        {
            Vector3Int temp = validPositions[i];
            int randomIndex = random.Next(i, validPositions.Count);
            validPositions[i] = validPositions[randomIndex];
            validPositions[randomIndex] = temp;
        }

        foreach (var position in validPositions)
        {
            if (respawnCount >= maxRespawnCount) break;

            if (random.NextDouble() < spawnSettings.animalSpawnChance)
            {
                SpawnedAnimal newAnimal = SpawnAnimalAtPosition(position, layerIndex);
                if (newAnimal != null)
                {
                    if (!spawnedAnimals.ContainsKey(layerIndex))
                        spawnedAnimals[layerIndex] = new List<SpawnedAnimal>();
                    spawnedAnimals[layerIndex].Add(newAnimal);
                    currentAnimals.Add(newAnimal); // Update current list
                    respawnCount++;
                }
            }
        }

        Debug.Log($"Respawned {respawnCount} animals on layer {layerIndex}");
    }

    /// <summary>
    /// Respawn all objects trên toàn bộ map cho layer chỉ định
    /// </summary>
    public void RespawnAllObjectsOnLayer(int layerIndex)
    {
        RespawnTreesOnLayer(layerIndex);
        RespawnBushesOnLayer(layerIndex);
        RespawnRocksOnLayer(layerIndex);
        RespawnAnimalsOnLayer(layerIndex);
    }

    /// <summary>
    /// Respawn all objects trên tất cả các layer
    /// </summary>
    public void RespawnAllObjectsOnAllLayers()
    {
        foreach (var layerGraph in GraphNode.Instance.layerGraphs)
        {
            RespawnAllObjectsOnLayer(layerGraph.Key);
        }
    }

    private List<Vector3Int> GetValidTreeRespawnPositions(int layerIndex, PathfindingGraph graph)
    {
        List<Vector3Int> validPositions = new List<Vector3Int>();
        HashSet<Vector3Int> occupiedPositions = GetOccupiedPositions(layerIndex);

        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            if (!IsValidSpawnNode(node, graph)) continue;
            if (occupiedPositions.Contains(node.position)) continue;

            float noiseValue = GetNoiseValue(node.position);
            if (noiseValue > spawnSettings.noiseThreshold)
            {
                validPositions.Add(node.position);
            }
        }

        return validPositions;
    }

    private List<Vector3Int> GetValidBushRespawnPositions(int layerIndex, PathfindingGraph graph)
    {
        List<Vector3Int> validPositions = new List<Vector3Int>();
        HashSet<Vector3Int> occupiedPositions = GetOccupiedPositions(layerIndex);

        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            if (!IsValidSpawnNode(node, graph)) continue;
            if (occupiedPositions.Contains(node.position)) continue;

            float bushNoise = GetBushNoiseValue(node.position);
            if (bushNoise > spawnSettings.bushNoiseThreshold)
            {
                validPositions.Add(node.position);
            }
        }

        return validPositions;
    }

    private List<Vector3Int> GetValidRockRespawnPositions(int layerIndex, PathfindingGraph graph)
    {
        List<Vector3Int> validPositions = new List<Vector3Int>();
        HashSet<Vector3Int> occupiedPositions = GetOccupiedPositions(layerIndex);

        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            if (!IsValidSpawnNode(node, graph)) continue;
            if (occupiedPositions.Contains(node.position)) continue;

            float rockNoise = GetRockNoiseValue(node.position);
            if (rockNoise > spawnSettings.rockNoiseThreshold)
            {
                validPositions.Add(node.position);
            }
        }

        return validPositions;
    }

    private List<Vector3Int> GetValidAnimalRespawnPositions(int layerIndex, PathfindingGraph graph)
    {
        List<Vector3Int> validPositions = new List<Vector3Int>();
        HashSet<Vector3Int> occupiedPositions = GetOccupiedPositions(layerIndex);

        List<SpawnedTree> trees = GetTreesOnLayer(layerIndex);
        List<SpawnedBush> bushes = GetBushesOnLayer(layerIndex);
        List<SpawnedRock> rocks = GetRocksOnLayer(layerIndex);

        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            if (!IsValidSpawnNode(node, graph)) continue;
            if (occupiedPositions.Contains(node.position)) continue;

            if (HasMinimumClearance(node.position, trees, bushes, rocks))
            {
                validPositions.Add(node.position);
            }
        }

        return validPositions;
    }

    private bool IsValidSpawnNode(Node node, PathfindingGraph graph)
    {
        if (!node.isWalkable || node.isBridge || node.isStair)
            return false;

        if (spawnSettings.avoidStairs && IsNearStairs(node, graph))
            return false;

        return true;
    }

    private HashSet<Vector3Int> GetOccupiedPositions(int layerIndex)
    {
        HashSet<Vector3Int> occupiedPositions = new HashSet<Vector3Int>();

        List<SpawnedTree> trees = GetTreesOnLayer(layerIndex);
        foreach (var tree in trees)
        {
            occupiedPositions.Add(tree.gridPosition);
        }

        List<SpawnedBush> bushes = GetBushesOnLayer(layerIndex);
        foreach (var bush in bushes)
        {
            occupiedPositions.Add(bush.gridPosition);
        }

        List<SpawnedRock> rocks = GetRocksOnLayer(layerIndex);
        foreach (var rock in rocks)
        {
            occupiedPositions.Add(rock.gridPosition);
        }

        List<SpawnedAnimal> animals = GetAnimalsOnLayer(layerIndex);
        foreach (var animal in animals)
        {
            occupiedPositions.Add(animal.gridPosition);
        }

        return occupiedPositions;
    }

    private TreeCluster FindSuitableCluster(Vector3Int position, int layerIndex)
    {
        if (!layerClusters.TryGetValue(layerIndex, out List<TreeCluster> clusters))
            return null;

        foreach (var cluster in clusters)
        {
            float distance = Vector3Int.Distance(position, cluster.centerPosition);
            if (distance <= spawnSettings.clusterRadius)
            {
                return cluster;
            }
        }

        return null;
    }

    private SpawnedTree SpawnTreeAtPosition(Vector3Int position, int layerIndex, TreeCluster cluster)
    {
        GameObject treePrefab = PrefabConfig.Instance.treePrefabs[random.Next(PrefabConfig.Instance.treePrefabs.Length)];
        Vector3 worldPosition = GridToWorld(position);

        var treeObj = PoolManager.Instance.Spawn(treePrefab, worldPosition, Quaternion.identity);

        if (treeObj.TryGetComponent<Tree>(out Tree treeComponent))
        {
            treeComponent.layerIndex = layerIndex;

            string layerName = $"Layer {layerIndex + 1}";
            int layerIndexMask = LayerMask.NameToLayer(layerName);
            treeObj.layer = layerIndexMask;

            treeComponent.positionInGrid = position;
            GraphNode.Instance.SetWalkableNode(position, layerIndex, false);
        }
        return new SpawnedTree(treeComponent, position, layerIndex, cluster);
    }

    public void RemoveDestroyedObject(Vector3Int position, int layerIndex, RespawnType objectType)
    {
        switch (objectType)
        {
            case RespawnType.Tree:
                if (spawnedTrees.TryGetValue(layerIndex, out List<SpawnedTree> trees))
                {
                    trees.RemoveAll(t => t.gridPosition == position);
                }
                break;
            case RespawnType.Bush:
                if (spawnedBushes.TryGetValue(layerIndex, out List<SpawnedBush> bushes))
                {
                    bushes.RemoveAll(b => b.gridPosition == position);
                }
                break;
            case RespawnType.Rock:
                if (spawnedRocks.TryGetValue(layerIndex, out List<SpawnedRock> rocks))
                {
                    rocks.RemoveAll(r => r.gridPosition == position);
                }
                break;
            case RespawnType.Animal:
                if (spawnedAnimals.TryGetValue(layerIndex, out List<SpawnedAnimal> animals))
                {
                    animals.RemoveAll(a => a.gridPosition == position);
                }
                break;
        }
    }
    #endregion

    #region Clear
    public void ClearAllObjects()
    {
        ClearAllTrees(); 

        foreach (var layerRocks in spawnedRocks.Values)
        {
            foreach (var rock in layerRocks)
            {
                if (rock.rockObject != null)
                {
                    if (spawnSettings.rocksBlockMovement)
                    {
                        GraphNode.Instance.SetWalkableNode(rock.gridPosition, rock.layerIndex, true);
                    }
                    DestroyImmediate(rock.rockObject);
                }
            }
        }

        foreach (var layerAnimals in spawnedAnimals.Values)
        {
            foreach (var animal in layerAnimals)
            {
                if (animal.animalObject != null)
                {
                    DestroyImmediate(animal.animalObject);
                }
            }
        }

        spawnedRocks.Clear();
        spawnedAnimals.Clear();
    }

    public void ClearObjectsOnLayer(int layerIndex)
    {
        ClearTreesOnLayer(layerIndex); 

        if (spawnedRocks.TryGetValue(layerIndex, out List<SpawnedRock> rocks))
        {
            foreach (var rock in rocks)
            {
                if (rock.rockObject != null)
                {
                    if (spawnSettings.rocksBlockMovement)
                    {
                        GraphNode.Instance.SetWalkableNode(rock.gridPosition, rock.layerIndex, true);
                    }
                    DestroyImmediate(rock.rockObject);
                }
            }
            spawnedRocks.Remove(layerIndex);
        }

        if (spawnedAnimals.TryGetValue(layerIndex, out List<SpawnedAnimal> animals))
        {
            foreach (var animal in animals)
            {
                if (animal.animalObject != null)
                {
                    DestroyImmediate(animal.animalObject);
                }
            }
            spawnedAnimals.Remove(layerIndex);
        }
    }

    public void ClearAllTrees()
    {
        foreach (var layerTrees in spawnedTrees.Values)
        {
            foreach (var tree in layerTrees)
            {
                if (tree.treeComponent != null)
                {
                    GraphNode.Instance.SetWalkableNode(tree.gridPosition, tree.layerIndex, true);
                    DestroyImmediate(tree.treeComponent.gameObject);
                }
            }
        }

        foreach (var layerBushes in spawnedBushes.Values)
        {
            foreach (var bush in layerBushes)
            {
                if (bush.bushObject != null)
                {
                    if (spawnSettings.bushesBlockMovement)
                    {
                        GraphNode.Instance.SetWalkableNode(bush.gridPosition, bush.layerIndex, true);
                    }
                    DestroyImmediate(bush.bushObject);
                }
            }
        }

        spawnedTrees.Clear();
        spawnedBushes.Clear();
        layerClusters.Clear();
    }

    private void ClearTreesOnLayer(int layerIndex)
    {
        if (spawnedTrees.TryGetValue(layerIndex, out List<SpawnedTree> trees))
        {
            foreach (var tree in trees)
            {
                if (tree.treeComponent != null)
                {
                    GraphNode.Instance.SetWalkableNode(tree.gridPosition, tree.layerIndex, true);
                    DestroyImmediate(tree.treeComponent.gameObject);
                }
            }

            spawnedTrees.Remove(layerIndex);
        }

        if (spawnedBushes.TryGetValue(layerIndex, out List<SpawnedBush> bushes))
        {
            foreach (var bush in bushes)
            {
                if (bush.bushObject != null)
                {
                    if (spawnSettings.bushesBlockMovement)
                    {
                        GraphNode.Instance.SetWalkableNode(bush.gridPosition, bush.layerIndex, true);
                    }
                    DestroyImmediate(bush.bushObject);
                }
            }

            spawnedBushes.Remove(layerIndex);
        }

        layerClusters.Remove(layerIndex);
    }
    #endregion

    #region Helpers
    private bool IsPositionOccupied(Vector3Int position, int layerIndex)
    {
        if (occupiedPositions.ContainsKey(layerIndex))
        {
            return occupiedPositions[layerIndex].Contains(position);
        }
        return false;
    }

    public List<SpawnedTree> GetTreesOnLayer(int layerIndex)
    {
        return spawnedTrees.TryGetValue(layerIndex, out List<SpawnedTree> trees) ? trees : new List<SpawnedTree>();
    }

    private List<SpawnedBush> GetBushesOnLayer(int layerIndex)
    {
        return spawnedBushes.TryGetValue(layerIndex, out List<SpawnedBush> bushes) ? bushes : new List<SpawnedBush>();
    }

    public List<TreeCluster> GetClustersOnLayer(int layerIndex)
    {
        return layerClusters.TryGetValue(layerIndex, out List<TreeCluster> clusters) ? clusters : new List<TreeCluster>();
    }

    private List<SpawnedRock> GetRocksOnLayer(int layerIndex)
    {
        return spawnedRocks.TryGetValue(layerIndex, out List<SpawnedRock> rocks) ? rocks : new List<SpawnedRock>();
    }

    private List<SpawnedAnimal> GetAnimalsOnLayer(int layerIndex)
    {
        return spawnedAnimals.TryGetValue(layerIndex, out List<SpawnedAnimal> animals) ? animals : new List<SpawnedAnimal>();
    }

    public Vector3 GridToWorld(Vector3Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);
    }

    #endregion

    #region Maintainance Resource

    #region Handle Event & Midnight Maintenance

    private void HandleHourChangedBroadcast(int hour)
    {
        if (hour == respawnHour) ExecuteMidnightResourceRecycling();
    }

    private void ExecuteMidnightResourceRecycling()
    {
        Debug.Log("[ObjectSpawner] 🕒 Điểm 3h sáng - Bắt đầu chu kỳ xử lý tuần hoàn tài nguyên sinh thái...");

        _bushDayTracker++;

        // --------------------------------------------------
        // CHU KỲ 1: CÂY CỐI & ĐỘNG VẬT (1 Ngày 1 lần)
        // --------------------------------------------------
        ExecuteMidnightTreeRespawn();
        ExecuteMidnightAnimalRespawn();

        // --------------------------------------------------
        // CHU KỲ 2: BỤI CỎ (2 Ngày 1 lần)
        // --------------------------------------------------
        if (_bushDayTracker >= 2)
        {
            ExecuteMidnightBushRespawn();
            _bushDayTracker = 0;
        }
        else
        {
            Debug.Log($"[ObjectSpawner] Bụi cỏ cần chờ thêm (Tiến trình: {_bushDayTracker}/2 ngày). Khóa chu kỳ.");
        }
    }

    #endregion

    #region Chopped Tree & Animal Registry API

    public void RegisterChoppedTree(Tree tree)
    {
        if (tree != null && !choppedTreeQueue.Contains(tree)) choppedTreeQueue.Add(tree);
    }

    public void RegisterHarvestedBush(Bush bushComponent)
    {
        if (bushComponent == null) return;

        var layer = bushComponent.layerIndex;
        var gridPos = bushComponent.positionInGrid;

        if (occupiedPositions.TryGetValue(layer, out var occupied)) occupied.Remove(gridPos);

        RemoveDestroyedObject(gridPos, layer, RespawnType.Bush);

        PoolManager.Instance.Despawn(bushComponent.gameObject);

        harvestedBushLayerQueue.Add(layer);

        Debug.Log(
            $"[ObjectSpawner] Đã thu hồi Bụi Cỏ tại ô {gridPos}. Đã đăng ký 1 suất tái sinh ngẫu nhiên cho Tầng {layer} sau 2 ngày.");
    }

    public void RegisterDeadAnimal(int layerIndex)
    {
        deadAnimalLayerQueue.Add(layerIndex);
    }

    #endregion

    #region Midnight Execution Subroutines

    private void ExecuteMidnightTreeRespawn()
    {
        if (choppedTreeQueue.Count == 0) return;

        Debug.Log($"[ObjectSpawner] Bắt đầu dọn dẹp và hồi phục {choppedTreeQueue.Count} cây...");

        var treesToProcess = new List<Tree>(choppedTreeQueue);
        choppedTreeQueue.Clear();

        var layersToRespawn = new HashSet<int>();

        foreach (var tree in treesToProcess)
        {
            if (tree == null) continue;

            var layer = tree.layerIndex;
            var gridPos = tree.positionInGrid;

            if (occupiedPositions.TryGetValue(layer, out var occupied)) occupied.Remove(gridPos);

            if (GraphNode.Instance != null)
            {
                GraphNode.Instance.SetWalkableNode(gridPos, layer, true);
            }
            RemoveDestroyedObject(gridPos, layer, RespawnType.Tree);
            PoolManager.Instance.Despawn(tree.gameObject);

            layersToRespawn.Add(layer);
        }

        foreach (var layerIdx in layersToRespawn) RespawnTreesOnLayer(layerIdx, 10);
    }

    private void ExecuteMidnightAnimalRespawn()
    {
        if (deadAnimalLayerQueue.Count == 0) return;

        Debug.Log($"[ObjectSpawner] Bắt đầu hồi sinh {deadAnimalLayerQueue.Count} cá thể động vật trên các tầng...");

        var respawnCountsPerLayer = new Dictionary<int, int>();
        foreach (var layer in deadAnimalLayerQueue)
            if (respawnCountsPerLayer.ContainsKey(layer)) respawnCountsPerLayer[layer]++;
            else respawnCountsPerLayer[layer] = 1;
        deadAnimalLayerQueue.Clear();

        foreach (var kvp in respawnCountsPerLayer) RespawnAnimalsOnLayer(kvp.Key, kvp.Value);
    }

    private void ExecuteMidnightBushRespawn()
    {
        if (harvestedBushLayerQueue.Count == 0) return;

        Debug.Log(
            $"[ObjectSpawner] Chu kỳ 2 ngày đã đến! Tiến hành hồi sinh ngẫu nhiên tổng cộng {harvestedBushLayerQueue.Count} bụi cỏ rải rác...");

        var respawnBushesPerLayer = new Dictionary<int, int>();
        foreach (var layer in harvestedBushLayerQueue)
            if (respawnBushesPerLayer.ContainsKey(layer)) respawnBushesPerLayer[layer]++;
            else respawnBushesPerLayer[layer] = 1;
        harvestedBushLayerQueue.Clear();

        foreach (var kvp in respawnBushesPerLayer) RespawnBushesOnLayer(kvp.Key, kvp.Value);
    }

    #endregion

    #endregion

    #region Save / Load System Extension

    public void PopulateSpawnerSaveData(GameSaveData gameSaveData)
    {
        var data = new SpawnerCycleSaveData();
        data.bushDayTracker = _bushDayTracker;

        foreach (var tree in choppedTreeQueue)
        {
            if (tree == null) continue;
            data.choppedTrees.Add(new ChoppedTreeSaveEntry
            {
                treeUniqueId = tree.GetComponent<UniqueId>()?.Id ?? "",
                gridPosition = tree.positionInGrid,
                layerIndex = tree.layerIndex
            });
        }

        data.harvestedBushLayers = new List<int>(harvestedBushLayerQueue);
        data.deadAnimalLayers = new List<int>(deadAnimalLayerQueue);

        gameSaveData.spawnerCycleData = data;
    }

    public void LoadSpawnerFromSaveData(GameSaveData gameSaveData)
    {
        var data = gameSaveData.spawnerCycleData;
        if (data == null) return;

        _bushDayTracker = data.bushDayTracker;

        choppedTreeQueue.Clear();
        harvestedBushLayerQueue.Clear();
        deadAnimalLayerQueue.Clear();

        if (data.deadAnimalLayers != null) deadAnimalLayerQueue = new List<int>(data.deadAnimalLayers);
        if (data.harvestedBushLayers != null) harvestedBushLayerQueue = new List<int>(data.harvestedBushLayers);

        _tempChoppedTreeData = data.choppedTrees != null
            ? new List<ChoppedTreeSaveEntry>(data.choppedTrees)
            : new List<ChoppedTreeSaveEntry>();
    }

    public void LinkChoppedTreesOnMapLoaded()
    {
        if (_tempChoppedTreeData == null || _tempChoppedTreeData.Count == 0) return;

        Debug.Log(
            $"[ObjectSpawner] Bắt đầu liên kết lại {_tempChoppedTreeData.Count} gốc cây mục vào hàng chờ tái sinh...");

        var allUniqueIds = FindObjectsOfType<UniqueId>();

        foreach (var treeEntry in _tempChoppedTreeData)
        {
            var matchedComponent = allUniqueIds.FirstOrDefault(u => u.Id == treeEntry.treeUniqueId);
            if (matchedComponent != null && matchedComponent.TryGetComponent<Tree>(out var runtimeTree))
                choppedTreeQueue.Add(runtimeTree);
        }

        _tempChoppedTreeData.Clear();
        Debug.Log(
            $"[ObjectSpawner] Kết nối thành công! Số lượng cây trong hàng chờ hiện tại: {choppedTreeQueue.Count}");
    }

    #endregion
}

