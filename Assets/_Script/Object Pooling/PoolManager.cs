using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [Header("Prewarm Settings")]
    [SerializeField] private List<PoolConfig> poolsToPrewarm;

    private Dictionary<GameObject, ObjectPool> pools 
        = new Dictionary<GameObject, ObjectPool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var config in poolsToPrewarm)
        {
            if (config.prefab == null) continue;

            CreatePool(config.prefab, config.prewarmCount);
        }
    }

    private void CreatePool(GameObject prefab, int prewarmCount = 0)
    {
        if (pools.ContainsKey(prefab)) return;

        GameObject parentObj = new GameObject(prefab.name + "_Pool");
        parentObj.transform.SetParent(transform);

        ObjectPool pool = new ObjectPool(prefab, parentObj.transform);
        pool.Prewarm(prewarmCount);

        pools.Add(prefab, pool);
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab, 0); 
        }

        return pools[prefab].Get(pos, rot);
    }

    public void Despawn(GameObject obj)
    {
        PoolIdentity identity = obj.GetComponent<PoolIdentity>();

        if (identity == null)
        {
            Debug.LogWarning("Object is not pooled!");
            Destroy(obj);
            return;
        }

        if (pools.TryGetValue(identity.Prefab, out ObjectPool pool))
        {
            pool.Return(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
}

[Serializable]
public class PoolConfig
{
    public GameObject prefab;
    public int prewarmCount = 10;
}