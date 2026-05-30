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

    private bool hasSpawned = false;

    private void Update()
    {
        float currentTime = TimeOfDaySystem.Instance.GetCurrentTime();

        if (currentTime >= 0f && currentTime < 0.5f)
        {
            if (!hasSpawned)
            {
                StartSpawningAll();
                hasSpawned = true;
            }
        }
        else if (currentTime >= 0.5f)
        {
            hasSpawned = false;
        }
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
        
        Debug.Log("Đã hoàn thành spawn toàn bộ danh sách!");
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