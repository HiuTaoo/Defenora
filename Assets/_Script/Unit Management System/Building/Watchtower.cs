using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Watchtower : Building
{
    private void Awake()
    {
        buildingType = BuildingType.WatchTower;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (positionSpots == null) return;

        Gizmos.color = Color.green;
        foreach (Transform spot in positionSpots)
        {
            if (spot != null)
                Gizmos.DrawSphere(spot.position, 0.1f);
        }
    }
#endif
}
