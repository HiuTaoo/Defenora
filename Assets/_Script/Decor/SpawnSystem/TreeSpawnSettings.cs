using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class TreeSpawnSettings
{
    [Header("Noise Settings")]
    public float noiseScale = 0.1f;
    public float noiseThreshold = 0.3f;
    public Vector2 noiseOffset = Vector2.zero;

    [Header("Cluster Settings")]
    public int minClusterSize = 5;
    public int maxClusterSize = 15;
    public float clusterRadius = 3f;
    public float treeSpacing = 2f;

    [Header("Spawn Settings")]
    public float spawnDensity = 0.7f;
    public int maxTreesPerLayer = 50;
    public bool avoidStairs = true;
    public float stairAvoidanceRadius = 2f;

    [Header("Tree Prefabs")]
    public GameObject[] treePrefabs;
}