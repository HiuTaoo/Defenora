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
            ObjectSpawner.Instance?.SpawnObjectsOnAllLayers();
        }

        if (Input.GetKeyDown(clearKey))
        {
            ObjectSpawner.Instance?.ClearAllTrees();
        }

/*        if (Input.GetKeyDown(debugKey))
        {
            ShowRuntimeDebugInfo();
        }*/
    }

    private void ShowRuntimeDebugInfo()
    {
        if (ObjectSpawner.Instance == null) return;

        Debug.Log("=== RUNTIME DEBUG INFO ===");

        int totalTrees = 0;
        int totalClusters = 0;

        foreach (var layerGraph in GraphNode.Instance.layerGraphs)
        {
            int layerIndex = layerGraph.Key;
            var trees = ObjectSpawner.Instance.GetTreesOnLayer(layerIndex);
            var clusters = ObjectSpawner.Instance.GetClustersOnLayer(layerIndex);

            totalTrees += trees.Count;
            totalClusters += clusters.Count;

            Debug.Log($"Layer {layerIndex}: {trees.Count} trees, {clusters.Count} clusters");
        }

        Debug.Log($"Total: {totalTrees} trees in {totalClusters} clusters");
    }
}
