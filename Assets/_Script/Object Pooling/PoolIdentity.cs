using UnityEngine;

public class PoolIdentity : MonoBehaviour
{
    public GameObject Prefab { get; private set; }

    public void Init(GameObject prefab)
    {
        Prefab = prefab;
    }
}