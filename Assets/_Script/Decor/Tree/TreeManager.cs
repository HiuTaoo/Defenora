using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeManager : MonoBehaviour
{
    public static TreeManager Instance;

    [Header("ListTree")]
    public List<Tree> listTree = new List<Tree>();

    [Header("Tree Prefab")]
    public GameObject[] treePrefabs;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
}

