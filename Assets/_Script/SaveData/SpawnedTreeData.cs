using UnityEngine;
[System.Serializable]
public class SpawnedTreeData
{
    public string id;
    public Vector3Int gridPosition;
    public int layerIndex;
    public int prefabIndex; 
    public TreeState treeState;
    public int currentChopHit;
    public int maxChopHit;
    public int parentClusterIndex = -1; 

    public SpawnedTreeData(SpawnedTree spawnedTree, int prefabIdx, int clusterIdx, string treeId)
    {
        gridPosition = spawnedTree.gridPosition;
        layerIndex = spawnedTree.layerIndex;
        prefabIndex = prefabIdx;
        parentClusterIndex = clusterIdx;
        id = treeId;

        if (spawnedTree.treeComponent != null)
        {
            treeState = spawnedTree.treeComponent.treeState;
            currentChopHit = spawnedTree.treeComponent.currentChopHit;
            maxChopHit = spawnedTree.treeComponent.maxChopHit;
        }
    }
}