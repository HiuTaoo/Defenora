using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner Instance;

    [SerializeField] public ObjectSpawnSettings spawnSettings;

    public Dictionary<int, List<TreeCluster>> layerClusters = new Dictionary<int, List<TreeCluster>>();
    public Dictionary<int, List<SpawnedTree>> spawnedTrees = new Dictionary<int, List<SpawnedTree>>();
    public Dictionary<int, List<SpawnedBush>> spawnedBushes = new Dictionary<int, List<SpawnedBush>>();

    private System.Random random;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            random = new System.Random();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        /*if (GraphNode.Instance != null)
        {
            StartCoroutine(DelayedSpawn());
        }*/
    }

    private System.Collections.IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(0.1f);
        SpawnTreesOnAllLayers();
    }

    public void SpawnTreesOnAllLayers()
    {
        foreach (var layerGraph in GraphNode.Instance.layerGraphs)
        {
            SpawnTreesOnLayer(layerGraph.Key);
        }
    }

    public void SpawnTreesOnLayer(int layerIndex)
    {
        if (!GraphNode.Instance.layerGraphs.TryGetValue(layerIndex, out PathfindingGraph graph))
        {
            Debug.LogWarning($"Không tìm thấy graph cho layer {layerIndex}");
            return;
        }

        //Debug.Log($"Bắt đầu spawn cây cho layer {layerIndex}");

        List<Vector3Int> potentialSpawnPoints = GetPotentialSpawnPoints(graph);

        List<TreeCluster> clusters = CreateNoiseClusters(potentialSpawnPoints, layerIndex);

        List<SpawnedTree> trees = SpawnTreesInClusters(clusters);

        List<SpawnedBush> bushes = SpawnBushesOnLayer(layerIndex, trees, clusters);

        layerClusters[layerIndex] = clusters;
        spawnedTrees[layerIndex] = trees;
        spawnedBushes[layerIndex] = bushes;

        //Debug.Log($"Đã spawn {trees.Count} cây và {bushes.Count} bụi cỏ trong {clusters.Count} clusters cho layer {layerIndex}");
    }

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

            GameObject treePrefab = spawnSettings.treePrefabs[random.Next(spawnSettings.treePrefabs.Length)];
            Vector3 worldPosition = GridToWorld(position);

            GameObject treeObj = Instantiate(treePrefab, worldPosition, Quaternion.identity, this.transform);

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
            if (spriteRenderer != null)
            {
                RenderManager.Instance.SetSortingOrderByIndex(RenderManager.Instance.decorRender, spriteRenderer, treeComponent.layerIndex);
            }

            SpawnedTree spawnedTree = new SpawnedTree(treeComponent, position, cluster.layerIndex, cluster);
            trees.Add(spawnedTree);
            treesSpawned++;
        }

        return trees;
    }

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
        if (spawnSettings.bushPrefabs == null || spawnSettings.bushPrefabs.Length == 0)
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
            List<SpawnedBush> clusterBushes = SpawnBushesInCluster(cluster, trees, graph);
            bushes.AddRange(clusterBushes);
        }

        // Spawn scattered bushes outside clusters
        List<SpawnedBush> scatteredBushes = SpawnScatteredBushes(layerIndex, trees, clusters, graph);
        bushes.AddRange(scatteredBushes);

        return bushes;
    }

    private List<SpawnedBush> SpawnBushesInCluster(TreeCluster cluster, List<SpawnedTree> trees, PathfindingGraph graph)
    {
        List<SpawnedBush> bushes = new List<SpawnedBush>();

        List<SpawnedTree> clusterTrees = trees.Where(t => t.parentCluster == cluster).ToList();

        foreach (var tree in clusterTrees)
        {
            List<Vector3Int> bushPositions = GetBushPositionsAroundTree(tree.gridPosition, graph);

            foreach (var bushPos in bushPositions)
            {
                if (IsTooCloseToTree(bushPos, trees) || IsTooCloseToBush(bushPos, bushes))
                    continue;

                if (random.NextDouble() < spawnSettings.bushSpawnChance)
                {
                    SpawnedBush bush = SpawnBushAtPosition(bushPos, cluster.layerIndex, cluster);
                    if (bush != null)
                    {
                        bushes.Add(bush);
                    }
                }
            }
        }

        return bushes;
    }

    private List<SpawnedBush> SpawnScatteredBushes(int layerIndex, List<SpawnedTree> trees, List<TreeCluster> clusters, PathfindingGraph graph)
    {
        List<SpawnedBush> bushes = new List<SpawnedBush>();

        HashSet<Vector3Int> occupiedPositions = new HashSet<Vector3Int>();
        foreach (var tree in trees)
        {
            occupiedPositions.Add(tree.gridPosition);
        }

        foreach (var cluster in clusters)
        {
            foreach (var pos in cluster.nodePositions)
            {
                occupiedPositions.Add(pos);
            }
        }

        List<Vector3Int> potentialBushPositions = new List<Vector3Int>();
        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;
            if (!node.isWalkable || node.isBridge || node.isStair) continue;

            if (occupiedPositions.Contains(node.position)) continue;

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
                if (!IsTooCloseToBush(pos, bushes))
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
        GameObject bushPrefab = spawnSettings.bushPrefabs[random.Next(spawnSettings.bushPrefabs.Length)];
        Vector3 worldPosition = GridToWorld(position);

        GameObject bushObj = Instantiate(bushPrefab, worldPosition, Quaternion.identity, this.transform);

        if (bushObj.TryGetComponent<Bush>(out Bush bushComponent))
        {
            bushComponent.layerIndex = layerIndex;

            string layerName = $"Layer {layerIndex + 1}";
            int layerIndexMask = LayerMask.NameToLayer(layerName);
            bushObj.layer = layerIndexMask;

            bushComponent.positionInGrid = position;

        }

        var spriteRenderer = bushObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            RenderManager.Instance.SetSortingOrderSubtractOneByIndex(RenderManager.Instance.decorRender, spriteRenderer, layerIndex);
        }

        return new SpawnedBush(bushObj, position, layerIndex, parentCluster);
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

    public void ClearTreesOnLayer(int layerIndex)
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

        if (layerClusters.ContainsKey(layerIndex))
        {
            layerClusters.Remove(layerIndex);
        }
    }

    public List<SpawnedTree> GetTreesOnLayer(int layerIndex)
    {
        return spawnedTrees.TryGetValue(layerIndex, out List<SpawnedTree> trees) ? trees : new List<SpawnedTree>();
    }

    public List<SpawnedBush> GetBushesOnLayer(int layerIndex)
    {
        return spawnedBushes.TryGetValue(layerIndex, out List<SpawnedBush> bushes) ? bushes : new List<SpawnedBush>();
    }

    public List<TreeCluster> GetClustersOnLayer(int layerIndex)
    {
        return layerClusters.TryGetValue(layerIndex, out List<TreeCluster> clusters) ? clusters : new List<TreeCluster>();
    }

    public Vector3 GridToWorld(Vector3Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);
    }

    
}