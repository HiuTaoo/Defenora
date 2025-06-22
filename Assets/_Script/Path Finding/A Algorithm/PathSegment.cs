using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathSegment
{
    public int layerIndex;
    public List<Vector3Int> positions;
    public string description;

    public PathSegment(int layer, List<Vector3Int> path, string desc)
    {
        layerIndex = layer;
        positions = new List<Vector3Int>(path);
        description = desc;
    }
}