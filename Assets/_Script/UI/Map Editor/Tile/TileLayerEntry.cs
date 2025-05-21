using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TileLayerEntry
{
    public string tileType;             // Ví dụ: "Grass", "Ground"
    public int layerIndex;              // Tầng thứ mấy: 0, 1, 2...
    public Tilemap tilemap;             // Tilemap tương ứng
}

