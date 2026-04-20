using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private GameObject prefab;
    private Queue<GameObject> objects = new Queue<GameObject>();
    private Transform parent;

    public int CountInactive => objects.Count;

    public ObjectPool(GameObject prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        if (objects.Count > 0)
        {
            obj = objects.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = GameObject.Instantiate(prefab, parent);
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
            GameObject obj = GameObject.Instantiate(prefab, parent);
            obj.AddComponent<PoolIdentity>().Init(prefab);
            obj.SetActive(false);
            objects.Enqueue(obj);
        }
    }

    public void Return(GameObject obj)
    {
        if (objects.Contains(obj)) return; 

        obj.GetComponent<IPoolable>()?.OnDespawned();

        obj.SetActive(false);
        
        if (parent != null)
        {
            obj.transform.SetParent(parent, false);
        }

        objects.Enqueue(obj);
    }
}