using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System;

public class MapLoader : MonoBehaviour
{
    public Grid grid;
    public List<Tilemap> layers; // Gán các Tilemap: Foam, Sand, Ground, Grass, Object
    public string saveFileName = "map.json";

    [Header("Tile mapping: Name ↔ TileBase")]
    public List<TileEntry> tileList;

    private Dictionary<string, TileBase> tileLookup;

    void Awake()
    {
        InitializeTileLookup();
        LoadMap();
    }

    private void InitializeTileLookup()
    {
        tileLookup = new Dictionary<string, TileBase>();
        foreach (var entry in tileList)
        {
            if (entry.tile == null)
            {
                Debug.LogWarning($"TileEntry '{entry.name}' has no TileBase assigned.");
                continue;
            }

            if (tileLookup.ContainsKey(entry.name))
            {
                Debug.LogError($"Duplicate tile name detected: {entry.name}. Ensure all tile names are unique.");
                continue;
            }

            tileLookup[entry.name] = entry.tile;
        }
    }

    public void LoadMap()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning("Map file not found at: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        MapData mapData = JsonUtility.FromJson<MapData>(json);

        ClearAllLayers();
        RestoreTiles(mapData);
        Debug.Log("Map loaded from: " + path);
    }

    private void ClearAllLayers()
    {
        foreach (Tilemap tm in layers)
        {
            if (tm != null)
                tm.ClearAllTiles();
            else
                Debug.LogWarning("A layer in the layers list is null.");
        }
    }

    private void RestoreTiles(MapData mapData)
    {
        foreach (TileData data in mapData.tiles)
        {
            Tilemap targetLayer = layers.Find(t => t != null && t.name == data.layerName);
            if (targetLayer == null)
            {
                Debug.LogWarning($"Layer '{data.layerName}' not found in layers list.");
                continue;
            }

            if (!tileLookup.TryGetValue(data.tileName, out TileBase tile))
            {
                Debug.LogWarning($"Tile '{data.tileName}' not found in tile lookup.");
                continue;
            }

            targetLayer.SetTile(data.position, tile);
        }
    }
}