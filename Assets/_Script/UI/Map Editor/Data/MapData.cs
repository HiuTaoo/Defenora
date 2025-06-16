using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TileData
{
    public string tileName;
    public Vector3Int position;
    public string layerName;

    public string tileType;

    public string metadata;
}


[Serializable]
public class MapData
{
    public List<TileData> tiles = new List<TileData>();
}

