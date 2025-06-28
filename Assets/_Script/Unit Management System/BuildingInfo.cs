using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

[System.Serializable]
public struct BuildingInfo
{
    public string stationName;
    public int currentCapacity;
    public int maxCapacity;
    public List<string> unitNames;
}