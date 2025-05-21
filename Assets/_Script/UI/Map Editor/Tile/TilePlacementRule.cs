using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "TileRules/Tile Placement Rule")]
public class TilePlacementRule : ScriptableObject
{
    public TileBase tile; 

    [Tooltip("Tên các Tilemap yêu cầu phải CÓ tile ở vị trí này để đặt được tile này.")]
    public List<string> requiredBaseTilemapNames;


}
