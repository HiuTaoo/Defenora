using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MenuTilesController : MonoBehaviour
{
    public static MenuTilesController Instance;


    [SerializeField] private Tilemap[] tilemap;
    [SerializeField] private TileBase[] tiles;
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    public Tilemap targetTilemap;
    public TileBase selectedTile;

    [SerializeField] private Vector3Int offset = Vector3Int.zero;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(Instance);
    }

}
