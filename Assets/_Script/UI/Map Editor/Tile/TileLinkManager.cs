using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileLinkManager : MonoBehaviour
{
    public static TileLinkManager Instance;

    public List<TileLink> tileLinks;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public TileLink GetLink(TileBase sourceTile, Tilemap sourceTilemap)
    {
        return tileLinks.Find(link =>
            link.sourceTile == sourceTile &&
            link.sourceTilemap == sourceTilemap);
    }
}
