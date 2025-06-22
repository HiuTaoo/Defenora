using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class LayerData 
{
    public Tilemap[] obstacleTilemap;
    public Tilemap[] walkableTilemap;
    public Tilemap stairTilemap;

    public int layerIndex = 0;
}
