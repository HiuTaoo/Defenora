using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedBush
{
    public GameObject bushObject;
    public Vector3Int gridPosition;
    public int layerIndex;
    public TreeCluster parentCluster; 

    public SpawnedBush(GameObject obj, Vector3Int pos, int layer, TreeCluster cluster)
    {
        bushObject = obj;
        gridPosition = pos;
        layerIndex = layer;
        parentCluster = cluster;
    }
}