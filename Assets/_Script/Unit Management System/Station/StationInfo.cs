using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

[System.Serializable]
public struct StationInfo
{
    public string stationName;
    public Station.StationType stationType;
    public int currentCapacity;
    public int maxCapacity;
    public List<string> unitNames;
}