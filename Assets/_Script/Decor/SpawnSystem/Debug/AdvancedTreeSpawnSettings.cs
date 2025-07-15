using UnityEngine;

[System.Serializable]
public class AdvancedTreeSpawnSettings
{
    [Header("Biome-based Settings")]
    public BiomeSettings[] biomeSettings;

    [Header("Seasonal Variations")]
    public bool enableSeasonalVariations = false;
    public float seasonalDensityMultiplier = 1f;

    [Header("Performance")]
    public bool enableLOD = true;
    public float lodDistance = 50f;
    public bool enableOcclusion = true;
}