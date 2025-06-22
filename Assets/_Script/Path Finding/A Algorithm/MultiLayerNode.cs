using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiLayerNode : IComparable<MultiLayerNode>
{
    public Node node;
    public float gCost;
    public float hCost;
    public float FCost => gCost + hCost;
    public MultiLayerNode parent;
    public bool isStairTransition;

    public MultiLayerNode(Node n)
    {
        node = n;
        gCost = float.MaxValue;
        hCost = 0;
        parent = null;
        isStairTransition = false;
    }

    public int CompareTo(MultiLayerNode other)
    {
        int fCompare = FCost.CompareTo(other.FCost);
        if (fCompare != 0) return fCompare;

        // Tie-breaker: ưu tiên hCost nhỏ hơn
        int hCompare = hCost.CompareTo(other.hCost);
        if (hCompare != 0) return hCompare;

        // Cuối cùng, ưu tiên node ở vị trí "bên trái, dưới" (đảm bảo unique)
        int xCompare = node.position.x.CompareTo(other.node.position.x);
        if (xCompare != 0) return xCompare;
        return node.position.y.CompareTo(other.node.position.y);
    }

    public override bool Equals(object obj)
    {
        return obj is MultiLayerNode other &&
               node.layerIndex == other.node.layerIndex &&
               node.position == other.node.position;
    }

    public override int GetHashCode()
    {
        return node.layerIndex.GetHashCode() ^ node.position.GetHashCode();
    }
}
