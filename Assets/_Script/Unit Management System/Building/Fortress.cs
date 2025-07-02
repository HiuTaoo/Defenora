using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Fortress : Building
{
    public Transform archer;

    private void Awake()
    {
        buildingType = BuildingType.Fortress;
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
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
