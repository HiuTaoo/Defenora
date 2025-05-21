using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TileLink
{
    public TileBase sourceTile;     
    public TileBase linkedTile;
    public Tilemap sourceTilemap;
    public Tilemap linkedTilemap;    
}

