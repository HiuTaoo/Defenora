#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TreeSpawner))]
public class TreeSpawnerEditor : Editor
{
    private TreeSpawner spawner;
    
    private void OnEnable()
    {
        spawner = (TreeSpawner)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Spawn Trees on All Layers"))
        {
            spawner.SpawnTreesOnAllLayers();
        }
        
        if (GUILayout.Button("Clear All Trees"))
        {
            spawner.ClearAllTrees();
        }
        
        GUILayout.Space(10);
        
        // Layer-specific controls
        if (GraphNode.Instance != null)
        {
            EditorGUILayout.LabelField("Layer Controls:", EditorStyles.boldLabel);
            
            foreach (var layerGraph in GraphNode.Instance.layerGraphs)
            {
                int layerIndex = layerGraph.Key;
                
                GUILayout.BeginHorizontal();
                
                if (GUILayout.Button($"Spawn Layer {layerIndex}"))
                {
                    spawner.SpawnTreesOnLayer(layerIndex);
                }
                
                if (GUILayout.Button($"Clear Layer {layerIndex}"))
                {
                    spawner.ClearTreesOnLayer(layerIndex);
                }
                
                GUILayout.EndHorizontal();
                
                // Show stats
                var trees = spawner.GetTreesOnLayer(layerIndex);
                var clusters = spawner.GetClustersOnLayer(layerIndex);
                EditorGUILayout.LabelField($"  Trees: {trees.Count}, Clusters: {clusters.Count}");
            }
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Debug Info"))
        {
            ShowDebugInfo();
        }
    }
    
    private void ShowDebugInfo()
    {
        if (GraphNode.Instance == null)
        {
            Debug.LogWarning("GraphNode.Instance is null!");
            return;
        }
        
        Debug.Log("=== TREE SPAWNER DEBUG INFO ===");
        
        foreach (var layerGraph in GraphNode.Instance.layerGraphs)
        {
            int layerIndex = layerGraph.Key;
            var trees = spawner.GetTreesOnLayer(layerIndex);
            var clusters = spawner.GetClustersOnLayer(layerIndex);
            
            Debug.Log($"Layer {layerIndex}: {trees.Count} trees in {clusters.Count} clusters");
            
            foreach (var cluster in clusters)
            {
                Debug.Log($"  Cluster at {cluster.centerPosition}: {cluster.nodePositions.Count} nodes, {cluster.desiredTreeCount} desired trees");
            }
        }
    }
}
#endif