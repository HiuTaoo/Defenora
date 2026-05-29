using UnityEngine;
[System.Serializable]
public class SpawnedBushData
{
    public string id;
    public Vector3Int gridPosition;
    public int layerIndex;
    public int prefabIndex; 
    public int parentClusterIndex = -1; 

    public SpawnedBushData(SpawnedBush spawnedBush, int prefabIdx, int clusterIdx, string bushId)
    {
        gridPosition = spawnedBush.gridPosition;
        layerIndex = spawnedBush.layerIndex;
        prefabIndex = prefabIdx;
        parentClusterIndex = clusterIdx;
        id = bushId;
    }
}