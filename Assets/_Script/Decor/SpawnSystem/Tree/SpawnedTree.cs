using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedTree
{
    public Tree treeComponent;
    public Vector3Int gridPosition;
    public int layerIndex;
    public TreeCluster parentCluster;

    public SpawnedTree(Tree treeObject, Vector3Int pos, int layer, TreeCluster cluster)
    {
        treeComponent = treeObject;
        gridPosition = pos;
        layerIndex = layer;
        parentCluster = cluster;
    }
}

