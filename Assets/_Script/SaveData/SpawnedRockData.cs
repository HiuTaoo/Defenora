using UnityEngine;

[System.Serializable]
public class SpawnedRockData
{
    public string id;
    public Vector3Int gridPosition;
    public int layerIndex;
    public int prefabIndex;
    public int parentClusterIndex;


    public SpawnedRockData(SpawnedRock rock, int prefabIdx, int clusterIdx, string rockId)
    {
        gridPosition = rock.gridPosition;
        layerIndex = rock.layerIndex;
        prefabIndex = prefabIdx;
        parentClusterIndex = clusterIdx;
        id = rockId;
    }
}