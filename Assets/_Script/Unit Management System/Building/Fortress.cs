using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Fortress : Building
{
    private void Awake()
    {
        buildingType = BuildingType.Fortress;
        RegisterSpot();
    }

}
