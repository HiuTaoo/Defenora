#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObjectSpawner))]
public class TreeSpawnerEditor : Editor
{
    private ObjectSpawner spawner;
    
    private void OnEnable()
    {
        spawner = (ObjectSpawner)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Spawn Objects on All Layers"))
        {
            spawner.SpawnObjectsOnAllLayers();
        }
        
        if (GUILayout.Button("Clear All Object"))
        {
            spawner.ClearAllObjects();
        }

        if (GUILayout.Button("Respawn All Objects"))
        {
            spawner.RespawnAllObjectsOnAllLayers();
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
                    spawner.SpawnObjectsOnLayer(layerIndex);
                }
                
                if (GUILayout.Button($"Clear Layer {layerIndex}"))
                {
                    spawner.ClearObjectsOnLayer(layerIndex);
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