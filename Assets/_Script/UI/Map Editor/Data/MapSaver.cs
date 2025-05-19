using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System;
using static UnityEngine.EventSystems.EventTrigger;

[Serializable]
public class TileEntry
{
    public string name;
    public TileBase tile;

    // Loại tile để phân biệt chức năng
    public string tileType;
}


public class MapSaver : MonoBehaviour
{
    public Grid grid;
    public List<Tilemap> layers; // Gán các tilemap layer: Foam, Sand, Ground, Grass, Object
    public string saveFileName = "map.json";

    [Header("Tile mapping: Name ↔ TileBase")]
    public List<TileEntry> tileList;

    private Dictionary<TileBase, string> tileNameMap;

    void Awake()
    {
        tileNameMap = new Dictionary<TileBase, string>();
        foreach (var entry in tileList)
        {
            if (entry.tile != null && !tileNameMap.ContainsKey(entry.tile))
                tileNameMap[entry.tile] = entry.name;
        }
    }

    public void SaveMap()
    {
        MapData mapData = new MapData();

        foreach (var tilemap in layers)
        {
            BoundsInt bounds = tilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                TileBase tile = tilemap.GetTile(pos);
                TileEntry entry = tileList.Find(e => e.tile == tile);
                if (entry != null)
                {
                    mapData.tiles.Add(new TileData
                    {
                        tileName = entry.name,
                        tileType = entry.tileType, // ← thêm dòng này
                        position = pos,
                        layerName = tilemap.name
                    });
                }
            }
        }

        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, saveFileName), json);
        Debug.Log("Map saved to: " + Path.Combine(Application.persistentDataPath, saveFileName));
    }
}
