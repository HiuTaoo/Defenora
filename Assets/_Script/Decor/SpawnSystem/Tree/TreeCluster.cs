using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeCluster
{
    public Vector3Int centerPosition;
    public int layerIndex;
    public List<Vector3Int> nodePositions;
    public float noiseValue;
    public int desiredTreeCount;

    public TreeCluster(Vector3Int center, int layer, float noise)
    {
        centerPosition = center;
        layerIndex = layer;
        noiseValue = noise;
        nodePositions = new List<Vector3Int>();
        desiredTreeCount = 0;
    }
}