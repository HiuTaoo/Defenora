using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SpawnedTreeData
{
    public Vector3Int gridPosition;
    public int layerIndex;
    public int prefabIndex; 
    public TreeState treeState;
    public int currentChopHit;
    public int maxChopHit;
    public int parentClusterIndex = -1; 

    public SpawnedTreeData(SpawnedTree spawnedTree, int prefabIdx, int clusterIdx)
    {
        gridPosition = spawnedTree.gridPosition;
        layerIndex = spawnedTree.layerIndex;
        prefabIndex = prefabIdx;
        parentClusterIndex = clusterIdx;

        if (spawnedTree.treeComponent != null)
        {
            treeState = spawnedTree.treeComponent.treeState;
            currentChopHit = spawnedTree.treeComponent.currentChopHit;
            maxChopHit = spawnedTree.treeComponent.maxChopHit;
        }
    }
}