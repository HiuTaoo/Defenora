using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Points Management")]
    [Tooltip("Kéo tất cả các SpawnPoint trên Map vào đây, hoặc để trống để tự động quét lúc Start")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("Difficulty Curve Settings")]
    [Tooltip("Số lượng quái cơ bản ở ngày 1")]
    [SerializeField] private int baseSpawnCount = 5;

    [Tooltip("Số lượng quái cộng thêm sau mỗi ngày tăng lên")]
    [SerializeField] private int countMultiplierPerDay = 3;

    private TimeOfDaySystem timeSystem;

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
            timeSystem.OnDayChanged += HandleNewDayWave;
            Debug.Log("[SpawnManager] 🟢 Kết nối ngày đêm thành công. Sẵn sàng điều phối các đợt quái!");
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
            timeSystem.OnDayChanged -= HandleNewDayWave;
        }
    }

    /// <summary>
    /// Xử lý phân bổ quái khi nhận tín hiệu ngày mới
    /// </summary>
    private void HandleNewDayWave(int newDay)
    {
        if (spawnPoints.Count == 0) return;

        int totalMonstersToSpawn = baseSpawnCount + (newDay * countMultiplierPerDay);

        Debug.Log($"[SpawnManager] ⚔️ ĐỢT QUÁI NGÀY {newDay} BẮT ĐẦU! Tổng số quái cần sinh: {totalMonstersToSpawn}");

        DistributeMonstersToPoints(totalMonstersToSpawn);
    }

    /// <summary>
    /// Thuật toán chia đều số lượng quái cho các cổng hiện có
    /// </summary>
    private void DistributeMonstersToPoints(int totalCount)
    {
        int pointCount = spawnPoints.Count;
        
        int baseShare = totalCount / pointCount;
        
        int remainder = totalCount % pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            int countForThisPoint = baseShare;
            if (i == 0)
            {
                countForThisPoint += remainder;
            }

            if (countForThisPoint > 0)
            {
                spawnPoints[i].OrderSpawnRandomly(countForThisPoint);
            }
        }
    }

    #region Cheat / Test Methods (Dành cho bạn debug nhanh)
    
    /// <summary>
    /// Hàm gọi thử một đợt quái ngay lập tức mà không cần chờ đổi ngày (Gắn vào nút UI Test)
    /// </summary>
    public void ForceSpawnWave(int customTotalCount)
    {
        Debug.Log($"[SpawnManager] 🛠️ Ép sinh đợt quái test với số lượng: {customTotalCount}");
        DistributeMonstersToPoints(customTotalCount);
    }

    #endregion
}