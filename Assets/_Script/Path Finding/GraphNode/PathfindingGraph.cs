using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingGraph 
{
    public Dictionary<Vector3Int, Node> nodes = new Dictionary<Vector3Int, Node>();
    public int layerIndex;
}
