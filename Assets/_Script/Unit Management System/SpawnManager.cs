using System;
using System.Collections.Generic;
using _Script.Object_Pooling;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public static event Action OnAllSpawnPointsDestroyed;

    [Header("Spawn Points Management")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("Difficulty Curve Settings")]
    [SerializeField] private int baseSpawnCount = 5;
    [SerializeField] private int countMultiplierPerDay = 3;
    [SerializeField] private float minDistanceFromPlayer = 15f;
    [SerializeField] private float maxSpawnRadius = 50f;
    [SerializeField] private int maxPlacementTries = 30;
    [SerializeField] private float minDistanceBetweenPoints = 10f;

    [Header("Difficulty Gate Settings")] [Tooltip("Số lượng cổng quái muốn sinh ra cố định")]
    public int targetSpawnPointCount = 2;

    private TimeOfDaySystem timeSystem;
    private int _monstersToSpawnTonight; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (spawnPoints.Count == 0)
        {
            spawnPoints.AddRange(FindObjectsOfType<SpawnPoint>());
            Debug.Log($"[SpawnManager] 🔍 Đã tự động tìm thấy {spawnPoints.Count} cổng sinh quái trên bản đồ.");
        }

        timeSystem = TimeOfDaySystem.Instance;
        if (timeSystem != null)
        {
            timeSystem.OnHourChanged += HandleHourTracking;
        }
    }

    private void OnDestroy()
    {
        if (timeSystem != null) timeSystem.OnHourChanged -= HandleHourTracking;
    }

    private void HandleHourTracking(int currentHour)
    {
        spawnPoints.RemoveAll(sp => sp == null);
        if (spawnPoints.Count == 0) return;

        var currentDay = timeSystem.CurrentDay;

        if (spawnPoints.Count == 1)
        {
            var singleGate = spawnPoints[0];
            if (currentHour == 0)
            {
                _monstersToSpawnTonight = baseSpawnCount + currentDay * countMultiplierPerDay;
                var countForFirstWave = _monstersToSpawnTonight / 2;
                singleGate.OrderSpawnRandomly(countForFirstWave);
            }
            else if (currentHour == 1)
            {
                var countForSecondWave = _monstersToSpawnTonight - _monstersToSpawnTonight / 2;
                if (countForSecondWave > 0) singleGate.OrderSpawnRandomly(countForSecondWave);
            }

            return; 
        }

        if (currentHour == 0)
        {
            _monstersToSpawnTonight = baseSpawnCount + currentDay * countMultiplierPerDay;
            var countForFirstGate = _monstersToSpawnTonight / 2;
            spawnPoints[0].OrderSpawnRandomly(countForFirstGate);
        }
        else if (currentHour == 1)
        {
            var countForSecondGate = _monstersToSpawnTonight - _monstersToSpawnTonight / 2;
            if (countForSecondGate > 0) spawnPoints[1].OrderSpawnRandomly(countForSecondGate);
        }
    }

    public void RemoveSpawnPoint(SpawnPoint spawnPoint)
    {
        if (spawnPoint == null) return;
        if (spawnPoints.Contains(spawnPoint)) spawnPoints.Remove(spawnPoint);

        if (spawnPoints.Count == 0) OnAllSpawnPointsDestroyed?.Invoke();
    }

    private void DistributeMonstersToPoints(int totalCount)
    {
        int pointCount = spawnPoints.Count;
        if (pointCount == 0) return;
        
        int baseShare = totalCount / pointCount;
        int remainder = totalCount % pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            int countForThisPoint = baseShare;
            if (i == 0) countForThisPoint += remainder;
            if (countForThisPoint > 0) spawnPoints[i].OrderSpawnRandomly(countForThisPoint);
        }
    }

    public bool GenerateSpawnPointsWithSafeZone(Vector3 playerPosition, int playerLayerIndex)
    {
        foreach (var existingSP in spawnPoints)
            if (existingSP != null)
                PoolManager.Instance.Despawn(existingSP.gameObject);

        spawnPoints.Clear();
        
        var prefabToUse = PrefabConfig.Instance.spawnPointPrefab;
        if (prefabToUse == null) return false;

        var playerGridPos = GraphNode.Instance.WorldToGridPos(playerPosition, playerLayerIndex);
        var pointsSpawnedSuccessfully = 0;
        var angleStep = Mathf.PI * 2f / targetSpawnPointCount;

        var alternateLayers = new List<int>();
        if (GraphNode.Instance != null && GraphNode.Instance.layerDatas != null)
        {
            var totalLayers = GraphNode.Instance.layerDatas.Length;
            for (var layer = 0; layer < totalLayers; layer++)
                if (layer != playerLayerIndex)
                    alternateLayers.Add(layer);
        }

        if (alternateLayers.Count == 0) alternateLayers.Add(playerLayerIndex);

        var currentMinDistBetweenPoints = minDistanceBetweenPoints;

        for (var i = 0; i < targetSpawnPointCount; i++)
        {
            var foundValidPosition = false;
            var finalSpawnPos = Vector3.zero;
            var targetLayerIndex = alternateLayers[Random.Range(0, alternateLayers.Count)];
            
            var minAngleForThisPoint = i * angleStep;
            var maxAngleForThisPoint = (i + 1) * angleStep;

            for (var tryCount = 0; tryCount < maxPlacementTries; tryCount++)
            {
                if (tryCount > maxPlacementTries / 2)
                    currentMinDistBetweenPoints = Mathf.Max(3f, currentMinDistBetweenPoints - 1f);

                var angle = Random.Range(minAngleForThisPoint, maxAngleForThisPoint);
                var radius = Random.Range(minDistanceFromPlayer, maxSpawnRadius);

                float posX = Mathf.RoundToInt(playerPosition.x + Mathf.Cos(angle) * radius);
                float posY = Mathf.RoundToInt(playerPosition.y + Mathf.Sin(angle) * radius);
                var candidatePos = new Vector3(posX + 0.5f, posY + 0.5f, 0f);
                var candidateGridPos = GraphNode.Instance.WorldToGridPos(candidatePos, targetLayerIndex);

                if (targetLayerIndex == playerLayerIndex)
                {
                    var distanceToPlayer = Vector3.Distance(candidatePos, playerPosition);
                    if (distanceToPlayer < minDistanceFromPlayer) continue;
                }

                var tooCloseToOtherSpawnPoint = false;
                foreach (var existingSP in spawnPoints)
                {
                    if (existingSP == null) continue;
                    if (existingSP.layerIndex == targetLayerIndex)
                    {
                        var distanceToOtherSP = Vector3.Distance(candidatePos, existingSP.transform.position);
                        if (distanceToOtherSP < currentMinDistBetweenPoints)
                        {
                            tooCloseToOtherSpawnPoint = true;
                            break;
                        }
                    }
                }

                if (tooCloseToOtherSpawnPoint) continue;

                if (GraphNode.Instance != null)
                {
                    var node = GraphNode.Instance.GetNode(candidateGridPos, targetLayerIndex);
                    if (node == null || !node.isWalkable) continue;
                }

                if (PathfindingAlgorithm.Instance != null)
                {
                    var testPath = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                        candidateGridPos, targetLayerIndex,
                        playerGridPos, playerLayerIndex
                    );
                    if (testPath == null || testPath.segments.Count == 0) continue;
                }

                finalSpawnPos = candidatePos;
                foundValidPosition = true;
                break;
            }

            if (foundValidPosition)
            {
                var spObj = PoolManager.Instance.Spawn(prefabToUse, finalSpawnPos, Quaternion.identity);
                if (spObj != null)
                {
                    var spComp = spObj.GetComponent<SpawnPoint>();
                    if (spComp != null)
                    {
                        spComp.layerIndex = targetLayerIndex;
                        spObj.name = $"Procedural_SpawnPoint_Layer{targetLayerIndex}_{pointsSpawnedSuccessfully}";

                        spawnPoints.Add(spComp);
                        spObj.transform.SetParent(transform);
                        pointsSpawnedSuccessfully++;
                    }
                }
            }

            currentMinDistBetweenPoints = minDistanceBetweenPoints;
        }

        return pointsSpawnedSuccessfully == targetSpawnPointCount;
    }

    public int CalculateEnemySpawnTonight()
    {
        var currentDay = timeSystem.CurrentDay;
        _monstersToSpawnTonight = baseSpawnCount + currentDay * countMultiplierPerDay;
        return _monstersToSpawnTonight;
    }

    public void ForceSpawnWave(int customTotalCount)
    {
        DistributeMonstersToPoints(customTotalCount);
    }
}