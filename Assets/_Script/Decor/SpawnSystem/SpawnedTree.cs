using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedTree
{
    public GameObject treeObject;
    public Vector3Int gridPosition;
    public int layerIndex;
    public TreeCluster parentCluster;

    public SpawnedTree(GameObject obj, Vector3Int pos, int layer, TreeCluster cluster)
    {
        treeObject = obj;
        gridPosition = pos;
        layerIndex = layer;
        parentCluster = cluster;
    }
}

