using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class ObjectSpawnSettings
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

    [Header("Bush Settings")]
    public GameObject[] bushPrefabs;

    [Header("Bush Spawn Settings")]
    [Range(0f, 1f)]
    public float bushSpawnChance = 0.4f; // Chance để spawn bush xung quanh cây

    [Range(0f, 1f)]
    public float scatteredBushSpawnChance = 0.1f; // Chance để spawn bush rải rác

    public float bushSpacing = 1f; // Khoảng cách tối thiểu giữa các bush
    public float bushToTreeMinDistance = 0.8f; // Khoảng cách tối thiểu từ bush đến cây
    public int bushAroundTreeRadius = 2; // Bán kính spawn bush xung quanh cây
    public bool bushesBlockMovement = false; // Bush có block movement không

    [Header("Bush Noise Settings")]
    public float bushNoiseScale = 0.15f;
    public float bushNoiseThreshold = 0.25f;
    public Vector2 bushNoiseOffset = new Vector2(100f, 100f); 

    [Header("Rock Prefabs")]
    public GameObject[] rockPrefabs;
}