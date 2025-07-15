using UnityEngine;

public class TreeSpawnerController : MonoBehaviour
{
    [Header("Runtime Controls")]
    public KeyCode respawnKey = KeyCode.R;
    public KeyCode clearKey = KeyCode.C;
    public KeyCode debugKey = KeyCode.D;

    private void Update()
    {
        if (Input.GetKeyDown(respawnKey))
        {
            TreeSpawner.Instance?.SpawnTreesOnAllLayers();
        }

        if (Input.GetKeyDown(clearKey))
        {
            TreeSpawner.Instance?.ClearAllTrees();
        }

/*        if (Input.GetKeyDown(debugKey))
        {
            ShowRuntimeDebugInfo();
        }*/
    }

    private void ShowRuntimeDebugInfo()
    {
        if (TreeSpawner.Instance == null) return;

        Debug.Log("=== RUNTIME DEBUG INFO ===");

        int totalTrees = 0;
        int totalClusters = 0;

        foreach (var layerGraph in GraphNode.Instance.layerGraphs)
        {
            int layerIndex = layerGraph.Key;
            var trees = TreeSpawner.Instance.GetTreesOnLayer(layerIndex);
            var clusters = TreeSpawner.Instance.GetClustersOnLayer(layerIndex);

            totalTrees += trees.Count;
            totalClusters += clusters.Count;

            Debug.Log($"Layer {layerIndex}: {trees.Count} trees, {clusters.Count} clusters");
        }

        Debug.Log($"Total: {totalTrees} trees in {totalClusters} clusters");
    }
}
