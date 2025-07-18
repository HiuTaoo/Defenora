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
    public float bushSpawnChance = 0.4f;
    [Range(0f, 1f)]
    public float scatteredBushSpawnChance = 0.1f;
    public float bushSpacing = 1f;
    public float bushToTreeMinDistance = 0.8f;
    public int bushAroundTreeRadius = 2;
    public bool bushesBlockMovement = false;

    [Header("Bush Noise Settings")]
    public float bushNoiseScale = 0.15f;
    public float bushNoiseThreshold = 0.25f;
    public Vector2 bushNoiseOffset = new Vector2(100f, 100f);

    [Header("Rock Settings")]
    public GameObject[] rockPrefabs;

    [Header("Rock Spawn Settings")]
    [Range(0f, 1f)]
    public float rockSpawnChance = 0.3f;
    [Range(0f, 1f)]
    public float scatteredRockSpawnChance = 0.08f;
    public float rockSpacing = 1.5f;
    public int rockAroundTreeRadius = 2;
    public bool rocksBlockMovement = true;
    public float rockRespawnDelay = 0.3f;

    [Header("Rock Noise Settings")]
    public float rockNoiseScale = 0.12f;
    public float rockNoiseThreshold = 0.4f;
    public Vector2 rockNoiseOffset = new Vector2(200f, 200f);

    [Header("Animal Settings")]
    public GameObject[] animalPrefabs;

    [Header("Animal Spawn Settings")]
    [Range(0f, 1f)]
    public float animalSpawnChance = 0.15f;
    public float animalMinDistanceFromObstacles = 2f;
    public float animalMaxDistanceFromVegetation = 5f;
}