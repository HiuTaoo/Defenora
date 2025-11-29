using System.Collections.Generic;
[System.Serializable]

public class LayerSpawnData
{
    public int layerIndex;
    public List<TreeClusterData> clusters = new List<TreeClusterData>();
    public List<SpawnedTreeData> trees = new List<SpawnedTreeData>();
    public List<SpawnedBushData> bushes = new List<SpawnedBushData>();
    public List<SpawnedRockData> rocks = new List<SpawnedRockData>();
    public List<SpawnedAnimalData> animals = new List<SpawnedAnimalData>();
}