using System.Collections;
using System.Collections.Generic;
using _Script.Object_Pooling;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPoint : MonoBehaviour
{
    [Header("Settings")]
    public int layerIndex;
    public float spawnDelay = 1f;

    [Header("Spawn Settings")]
    [Tooltip("Danh sách các loại quái vật ĐƯỢC PHÉP xuất hiện tại cổng này")]
    public List<GameObject> allowedEnemyPrefabs = new List<GameObject>();

    /// <summary>
    /// Hàm này bây giờ sẽ do SpawnManager gọi xuống từ bên ngoài
    /// </summary>
    public void OrderSpawnRandomly(int count)
    {
        if (allowedEnemyPrefabs == null || allowedEnemyPrefabs.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] ⚠️ Không có Prefab quái nào được gán!");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SpawnRandomSequenceRoutine(count));
    }

    private IEnumerator SpawnRandomSequenceRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, allowedEnemyPrefabs.Count);
            GameObject chosenPrefab = allowedEnemyPrefabs[randomIndex];

            if (chosenPrefab != null)
            {
                SpawnObject(chosenPrefab);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
        Debug.Log($"[{gameObject.name}] ⚔️ Cổng đã hoàn thành sinh {count} quái vật theo lệnh.");
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