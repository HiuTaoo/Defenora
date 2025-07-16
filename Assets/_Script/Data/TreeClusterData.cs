using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeClusterData
{
    public Vector3Int centerPosition;
    public int layerIndex;
    public List<Vector3Int> nodePositions = new List<Vector3Int>();
    public float noiseValue;
    public int desiredTreeCount;

    public TreeClusterData(TreeCluster cluster)
    {
        centerPosition = cluster.centerPosition;
        layerIndex = cluster.layerIndex;
        nodePositions = new List<Vector3Int>(cluster.nodePositions);
        noiseValue = cluster.noiseValue;
        desiredTreeCount = cluster.desiredTreeCount;
    }

    public TreeCluster ToTreeCluster()
    {
        TreeCluster cluster = new TreeCluster(centerPosition, layerIndex, noiseValue);
        cluster.nodePositions = new List<Vector3Int>(nodePositions);
        cluster.desiredTreeCount = desiredTreeCount;
        return cluster;
    }
}