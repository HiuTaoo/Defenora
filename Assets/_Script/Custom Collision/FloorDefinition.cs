using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FloorDefinition
{
    public string floorName;
    public int floorIndex;
    public LayerMask collisionMask;
    public int sortingOrder;

    public LayerMask floorLayers;
}

