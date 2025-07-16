using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SpawnedBushData
{
    public Vector3Int gridPosition;
    public int layerIndex;
    public int prefabIndex; 
    public int parentClusterIndex = -1; 

    public SpawnedBushData(SpawnedBush spawnedBush, int prefabIdx, int clusterIdx)
    {
        gridPosition = spawnedBush.gridPosition;
        layerIndex = spawnedBush.layerIndex;
        prefabIndex = prefabIdx;
        parentClusterIndex = clusterIdx;
    }
}