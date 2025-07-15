using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3Int position;
    public int layerIndex;
    public bool isWalkable;
    public bool isStair ;
    public bool isBridge;
    public Node stairTargetNode;
    public List<Node> neighbors = new List<Node>();

    // Cho A* algorithm
    public float gCost;
    public float hCost;
    public float FCost => gCost + hCost;
    public Node parent;


}


