
using UnityEngine;

[System.Serializable]
public class BiomeSettings
{
    public string biomeName;
    public float densityMultiplier = 1f;
    public float noiseThresholdOverride = -1f; // -1 means use default
    public GameObject[] specificTreePrefabs;
    public Color debugColor = Color.white;
}