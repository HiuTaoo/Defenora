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
    [Tooltip("Kéo tất cả các SpawnPoint trên Map vào đây, hoặc để trống để tự động quét lúc Start")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("Difficulty Curve Settings")]
    [Tooltip("Số lượng quái cơ bản ở ngày 1")]
    [SerializeField] private int baseSpawnCount = 5;

    [Tooltip("Số lượng quái cộng thêm sau mỗi ngày tăng lên")]
    [SerializeField] private int countMultiplierPerDay = 3;

    [Tooltip("Bán kính tối thiểu (Safe Zone) bắt buộc phải tránh xa Player")] [SerializeField]
    private float minDistanceFromPlayer = 15f;

    [Tooltip("Bán kính tối đa từ tâm map có thể đặt cổng sinh quái")] [SerializeField]
    private float maxSpawnRadius = 50f;

    [Tooltip("Số lần thử bốc tọa độ tối đa trước khi chấp nhận thất bại (tránh treo game)")] [SerializeField]
    private int maxPlacementTries = 30;

    [Tooltip("Khoảng cách tối thiểu giữa các cổng SpawnPoint với nhau để tránh tụ tập một chỗ")] [SerializeField]
    private float minDistanceBetweenPoints = 10f;

    private TimeOfDaySystem timeSystem;
    private int _monstersToSpawnTonight; 

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
            Debug.Log("[SpawnManager] 🟢 Kết nối hệ thống giờ thành công. Sẵn sàng điều phối quái theo khung giờ đêm!");
        }
        else
        {
            Debug.LogError("[SpawnManager] ❌ Không tìm thấy TimeOfDaySystem.Instance!");
        }
    }

    private void OnDestroy()
    {
        if (timeSystem != null)
        {
            timeSystem.OnHourChanged -= HandleHourTracking;
        }
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

                Debug.LogWarning($"[SpawnManager] ⚠️ CHỈ CÒN 1 CỔNG [{singleGate.gameObject.name}]. " +
                                 $"🌑 00:00 ĐÊM: Kích hoạt ĐỢT 1 sinh {countForFirstWave} quái.");

                singleGate.OrderSpawnRandomly(countForFirstWave);
            }
            else if (currentHour == 1)
            {
                var countForSecondWave = _monstersToSpawnTonight - _monstersToSpawnTonight / 2;

                if (countForSecondWave > 0)
                {
                    Debug.LogWarning($"[SpawnManager] ⚠️ CHỈ CÒN 1 CỔNG [{singleGate.gameObject.name}]. " +
                                     $"⚔️ 01:00 ĐÊM: Kích hoạt ĐỢT 2 sinh nốt {countForSecondWave} quái.");

                    singleGate.OrderSpawnRandomly(countForSecondWave);
                }
            }

            return; // Ngắt luồng xử lý tại đây
        }

        if (currentHour == 0)
        {
            _monstersToSpawnTonight = baseSpawnCount + currentDay * countMultiplierPerDay;
            var countForFirstGate = _monstersToSpawnTonight / 2;

            var gate1Name = spawnPoints[0].gameObject.name;
            Debug.Log(
                $"[SpawnManager] 🌑 00:00 ĐÊM (Ngày {currentDay}): 🚨 CỔNG 1 [{gate1Name}] MỞ! Sinh trước {countForFirstGate} quái vật.");

            spawnPoints[0].OrderSpawnRandomly(countForFirstGate);
        }
        else if (currentHour == 1)
        {
            var countForSecondGate = _monstersToSpawnTonight - _monstersToSpawnTonight / 2;

            if (countForSecondGate > 0)
            {
                var gate2Name = spawnPoints[1].gameObject.name;
                Debug.Log(
                    $"[SpawnManager] ⚔️ 01:00 ĐÊM (Ngày {currentDay}): 🚨 CỔNG 2 [{gate2Name}] MỞ! Sinh nốt {countForSecondGate} quái vật.");

                spawnPoints[1].OrderSpawnRandomly(countForSecondGate);
            }
        }
    }

    public void RemoveSpawnPoint(SpawnPoint spawnPoint)
    {
        if (spawnPoint == null) return;

        if (spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Remove(spawnPoint);
            Debug.LogWarning(
                $"[SpawnManager] 💥 Đã gỡ bỏ [{spawnPoint.gameObject.name}] khỏi danh sách. Còn lại: {spawnPoints.Count} cổng.");
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("[SpawnManager] 🏆 HOÀN THÀNH CHIẾN DỊCH! Tất cả cổng quái trên hành tinh đã bị dọn sạch!");
            OnAllSpawnPointsDestroyed?.Invoke();
        }
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

            if (countForThisPoint > 0)
            {
                spawnPoints[i].OrderSpawnRandomly(countForThisPoint);
            }
        }
    }

    public bool GenerateSpawnPointsWithSafeZone(int numberOfPoints, Vector3 playerPosition, int playerLayerIndex)
    {
        var prefabToUse = PrefabConfig.Instance.spawnPointPrefab;
        if (prefabToUse == null) return false;

        var pointsSpawnedSuccessfully = 0;
        var playerGridPos = new Vector3Int(Mathf.FloorToInt(playerPosition.x), Mathf.FloorToInt(playerPosition.y), 0);
        var angleStep = Mathf.PI * 2f / numberOfPoints;

        for (var i = 0; i < numberOfPoints; i++)
        {
            var foundValidPosition = false;
            var finalSpawnPos = Vector3.zero;
            var targetLayerIndex = playerLayerIndex;
            var minAngleForThisPoint = i * angleStep;
            var maxAngleForThisPoint = (i + 1) * angleStep;

            for (var tryCount = 0; tryCount < maxPlacementTries; tryCount++)
            {
                var angle = Random.Range(minAngleForThisPoint, maxAngleForThisPoint);
                var radius = Random.Range(minDistanceFromPlayer, maxSpawnRadius);

                float posX = Mathf.RoundToInt(playerPosition.x + Mathf.Cos(angle) * radius);
                float posY = Mathf.RoundToInt(playerPosition.y + Mathf.Sin(angle) * radius);
                var candidatePos = new Vector3(posX + 0.5f, posY + 0.5f, 0f);
                var candidateGridPos =
                    new Vector3Int(Mathf.FloorToInt(candidatePos.x), Mathf.FloorToInt(candidatePos.y), 0);

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
                        if (distanceToOtherSP < minDistanceBetweenPoints)
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
        }
        return pointsSpawnedSuccessfully == numberOfPoints;
    }

    public int CalculateEnemySpawnTonight()
    {
        var currentDay = timeSystem.CurrentDay;
        _monstersToSpawnTonight = baseSpawnCount + currentDay * countMultiplierPerDay;
        return _monstersToSpawnTonight;
    }

    #region Cheat / Test Methods
    public void ForceSpawnWave(int customTotalCount)
    {
        Debug.Log($"[SpawnManager] 🛠️ Ép sinh đợt quái test với số lượng: {customTotalCount}");
        DistributeMonstersToPoints(customTotalCount);
    }
    #endregion
}