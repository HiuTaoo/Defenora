using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SpawnedRock
{
    public GameObject rockObject;
    public Vector3Int gridPosition;
    public int layerIndex;
    public TreeCluster parentCluster;

    public SpawnedRock(GameObject rockObj, Vector3Int gridPos, int layer, TreeCluster cluster)
    {
        rockObject = rockObj;
        gridPosition = gridPos;
        layerIndex = layer;
        parentCluster = cluster;
    }
}