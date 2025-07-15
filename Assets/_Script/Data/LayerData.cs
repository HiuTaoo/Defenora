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
    public Tilemap bridgeTilemap;

    public int layerIndex = 0;

    public void AddBridgeTilemapToWalkable()
    {
        if (bridgeTilemap == null)
        {
            UnityEngine.Debug.LogWarning($"[LayerData] BridgeTilemap is null for layer {layerIndex}");
            return;
        }

        if (walkableTilemap == null)
        {
            walkableTilemap = new Tilemap[] { bridgeTilemap };
            return;
        }

        int length = walkableTilemap.Length;
        Tilemap[] newArray = new Tilemap[length + 1];

        for (int i = 0; i < length; i++)
        {
            newArray[i] = walkableTilemap[i];
        }

        newArray[length] = bridgeTilemap;

        walkableTilemap = newArray;
    }

}
