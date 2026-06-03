using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private GameObject prefab;
    private Queue<GameObject> objects = new Queue<GameObject>();

    public int CountInactive => objects.Count;

    public Transform ParentTransform { get; }

    public ObjectPool(GameObject prefab, Transform parent)
    {
        this.prefab = prefab;
        this.ParentTransform = parent;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        if (objects.Count > 0)
        {
            obj = objects.Dequeue();
            if (obj == null) return Get(position, rotation);
            
            obj.SetActive(true);
        }
        else
        {
            obj = GameObject.Instantiate(prefab, ParentTransform);
            obj.AddComponent<PoolIdentity>().Init(prefab);
        }

        obj.transform.SetPositionAndRotation(position, rotation);

        obj.GetComponent<IPoolable>()?.OnSpawned();

        return obj;
    }
    
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject obj = GameObject.Instantiate(prefab, ParentTransform);
            obj.AddComponent<PoolIdentity>().Init(prefab);
            obj.SetActive(false);
            objects.Enqueue(obj);
        }
    }

    public void Return(GameObject obj)
    {
        if (obj == null || objects.Contains(obj)) return; 

        obj.GetComponent<IPoolable>()?.OnDespawned();

        obj.SetActive(false);
        
        if (ParentTransform != null)
        {
            obj.transform.SetParent(ParentTransform, false);
        }

        objects.Enqueue(obj);
    }

    public void ClearPool()
    {
        if (objects == null) return;

        while (objects.Count > 0)
        {
            var obj = objects.Dequeue();
            if (obj != null) Object.Destroy(obj);
        }

        objects.Clear();
    }
}