using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TileData
{
    public string tileName;
    public Vector3Int position;
    public string layerName;

    // Thêm loại tile: ví dụ "stair", "bridge", "object"
    public string tileType;

    // Có thể mở rộng thêm dữ liệu tùy chỉnh nếu cần
    public string metadata;
}


[Serializable]
public class MapData
{
    public List<TileData> tiles = new List<TileData>();
}

