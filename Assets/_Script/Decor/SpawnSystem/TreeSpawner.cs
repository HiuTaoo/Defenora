using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    public static TreeSpawner Instance;

    [SerializeField] public TreeSpawnSettings spawnSettings;

    public Dictionary<int, List<TreeCluster>> layerClusters = new Dictionary<int, List<TreeCluster>>();
    public Dictionary<int, List<SpawnedTree>> spawnedTrees = new Dictionary<int, List<SpawnedTree>>();
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
        if (GraphNode.Instance != null)
        {
            StartCoroutine(DelayedSpawn());
        }
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

        Debug.Log($"Bắt đầu spawn cây cho layer {layerIndex}");

        // Bước 1: Tạo noise map và tìm potential spawn points
        List<Vector3Int> potentialSpawnPoints = GetPotentialSpawnPoints(graph);

        // Bước 2: Tạo clusters dựa trên noise
        List<TreeCluster> clusters = CreateNoiseClusters(potentialSpawnPoints, layerIndex);

        // Bước 3: Spawn cây trong từng cluster
        List<SpawnedTree> trees = SpawnTreesInClusters(clusters);

        // Lưu kết quả
        layerClusters[layerIndex] = clusters;
        spawnedTrees[layerIndex] = trees;

        Debug.Log($"Đã spawn {trees.Count} cây trong {clusters.Count} clusters cho layer {layerIndex}");
    }

    private List<Vector3Int> GetPotentialSpawnPoints(PathfindingGraph graph)
    {
        List<Vector3Int> spawnPoints = new List<Vector3Int>();

        foreach (var kvp in graph.nodes)
        {
            Node node = kvp.Value;

            if (!node.isWalkable) continue;

            if(node.isBridge) continue;

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
            // Lấy điểm có noise value cao nhất làm center
            Vector3Int centerPoint = GetHighestNoisePoint(unprocessedPoints);
            float centerNoise = GetNoiseValue(centerPoint);

            TreeCluster cluster = new TreeCluster(centerPoint, layerIndex, centerNoise);

            // Tìm các điểm gần center để tạo cluster
            List<Vector3Int> clusterPoints = GetPointsInRadius(centerPoint, unprocessedPoints, spawnSettings.clusterRadius);

            // Giới hạn size cluster
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

            // Xóa các điểm đã được xử lý
            foreach (var point in clusterPoints)
            {
                unprocessedPoints.Remove(point);
            }

            // Xóa luôn center point nếu cluster không đủ lớn
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

        // Shuffle positions để random
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
                var layer = cluster.layerIndex + 1; 
                string layerName = $"Layer {layer}";
                int layerIndex = LayerMask.NameToLayer(layerName);
                treeObj.layer = layerIndex;
                GraphNode.Instance.SetWalkableNode(position, treeComponent.layerIndex, false);
            }

            var spriteRenderer = treeObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                RenderManager.Instance.SetSortingOrderByIndex(RenderManager.Instance.decorRender, spriteRenderer, treeComponent.layerIndex);
            }

            SpawnedTree spawnedTree = new SpawnedTree(treeObj, position, cluster.layerIndex, cluster);
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

    // Utility methods
    public void ClearAllTrees()
    {
        foreach (var layerTrees in spawnedTrees.Values)
        {
            foreach (var tree in layerTrees)
            {
                if (tree.treeObject != null)
                {
                    GraphNode.Instance.SetWalkableNode(tree.gridPosition, tree.layerIndex, true);
                    DestroyImmediate(tree.treeObject);
                }
            }
        }

        spawnedTrees.Clear();
        layerClusters.Clear();
    }

    public void ClearTreesOnLayer(int layerIndex)
    {
        if (spawnedTrees.TryGetValue(layerIndex, out List<SpawnedTree> trees))
        {
            foreach (var tree in trees)
            {
                if (tree.treeObject != null)
                {
                    GraphNode.Instance.SetWalkableNode(tree.gridPosition, tree.layerIndex, true);
                    DestroyImmediate(tree.treeObject);
                }
            }

            spawnedTrees.Remove(layerIndex);
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

    public List<TreeCluster> GetClustersOnLayer(int layerIndex)
    {
        return layerClusters.TryGetValue(layerIndex, out List<TreeCluster> clusters) ? clusters : new List<TreeCluster>();
    }

    private Vector3 GridToWorld(Vector3Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);
    }

}
