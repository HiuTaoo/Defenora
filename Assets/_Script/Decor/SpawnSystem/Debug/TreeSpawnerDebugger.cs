using UnityEngine;

public class TreeSpawnerDebugger : MonoBehaviour
{
    [Header("Debug Visualization")]
    public bool showClusters = true;
    public bool showSpawnPoints = true;
    public bool showNoiseValues = false;
    public Color clusterColor = Color.green;
    public Color spawnPointColor = Color.blue;
    public Color noiseColor = Color.red;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || TreeSpawner.Instance == null) return;

        if (showClusters)
        {
            DrawClusters();
        }

        if (showSpawnPoints)
        {
            DrawSpawnPoints();
        }

        if (showNoiseValues)
        {
            DrawNoiseValues();
        }
    }

    private void DrawClusters()
    {
        Gizmos.color = clusterColor;

        foreach (var layerClusters in TreeSpawner.Instance.layerClusters)
        {
            foreach (var cluster in layerClusters.Value)
            {
                Vector3 centerWorld = new Vector3(cluster.centerPosition.x, cluster.centerPosition.y, 0);
                Gizmos.DrawWireSphere(centerWorld, TreeSpawner.Instance.spawnSettings.clusterRadius);

                // Draw cluster nodes
                foreach (var nodePos in cluster.nodePositions)
                {
                    Vector3 nodeWorld = new Vector3(nodePos.x, nodePos.y, 0);
                    Gizmos.DrawWireCube(nodeWorld, Vector3.one * 0.2f);
                }
            }
        }
    }

    private void DrawSpawnPoints()
    {
        Gizmos.color = spawnPointColor;

        foreach (var layerTrees in TreeSpawner.Instance.spawnedTrees)
        {
            foreach (var tree in layerTrees.Value)
            {
                Vector3 treeWorld = new Vector3(tree.gridPosition.x, tree.gridPosition.y, 0);
                Gizmos.DrawSphere(treeWorld, 0.1f);
            }
        }
    }

    private void DrawNoiseValues()
    {
        if (GraphNode.Instance == null) return;

        Gizmos.color = noiseColor;

        foreach (var layerGraph in GraphNode.Instance.layerGraphs)
        {
            var graph = layerGraph.Value;

            foreach (var kvp in graph.nodes)
            {
                Node node = kvp.Value;
                if (!node.isWalkable) continue;

                float noiseValue = GetNoiseValue(node.position);
                if (noiseValue > TreeSpawner.Instance.spawnSettings.noiseThreshold)
                {
                    Vector3 worldPos = new Vector3(node.position.x, node.position.y, 0);
                    Gizmos.color = Color.Lerp(Color.white, noiseColor, noiseValue);
                    Gizmos.DrawCube(worldPos, Vector3.one * 0.1f);
                }
            }
        }
    }

    private float GetNoiseValue(Vector3Int position)
    {
        var settings = TreeSpawner.Instance.spawnSettings;
        float x = (position.x + settings.noiseOffset.x) * settings.noiseScale;
        float y = (position.y + settings.noiseOffset.y) * settings.noiseScale;

        return Mathf.PerlinNoise(x, y);
    }
}