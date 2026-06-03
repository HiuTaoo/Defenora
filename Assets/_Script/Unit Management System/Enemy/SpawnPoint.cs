using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Serializable]
    public struct SpawnData
    {
        public GameObject prefab;
        public int count;
    }

    [Header("Settings")]
    public int layerIndex;
    public float spawnDelay = 1f;

    [Header("Spawn List")]
    public List<SpawnData> spawnList = new List<SpawnData>();

    private TimeOfDaySystem timeSystem;

    private void Start()
    {
        timeSystem = TimeOfDaySystem.Instance;

        if (timeSystem != null)
        {
            timeSystem.OnDayChanged += HandleDayChanged;
            Debug.Log($"[{gameObject.name}] 🟢 Đã kết nối thành công với hệ thống ngày đêm. Đang chờ ngày mới...");
        }
        else
        {
            Debug.LogError(
                $"[{gameObject.name}] ❌ Không tìm thấy TimeOfDaySystem.Instance trên Scene để đăng ký gọi quái!");
        }
    }

    private void OnDestroy()
    {
        if (timeSystem != null)
        {
            timeSystem.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        Debug.Log($"[{gameObject.name}] 🌅 Nhận tín hiệu Ngày mới (Ngày {newDay})! Bắt đầu gọi quái xuất trận...");

        StartSpawningAll();
    }

    public void StartSpawningAll()
    {
        StopAllCoroutines();
        StartCoroutine(SpawnSequenceRoutine());
    }

    private IEnumerator SpawnSequenceRoutine()
    {
        foreach (SpawnData data in spawnList)
        {
            if (data.prefab == null || data.count <= 0) continue;

            for (int i = 0; i < data.count; i++)
            {
                SpawnObject(data.prefab);
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        Debug.Log($"[{gameObject.name}] Đã hoàn thành spawn toàn bộ danh sách quái cho ngày mới!");
    }

    private void SpawnObject(GameObject prefab)
    {
        var enemy = PoolManager.Instance.Spawn(prefab, transform.position, Quaternion.identity);
        var unit = enemy.GetComponent<Unit>();
        
        unit.characterMovement.CurrentLayer = layerIndex;
        unit.enemySpawnPoint = transform.gameObject;
        
        UnitManager.Instance.RegisterUnit(unit);
    }
}