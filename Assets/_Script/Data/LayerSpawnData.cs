using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class LayerSpawnData
{
    public int layerIndex;
    public List<TreeClusterData> clusters = new List<TreeClusterData>();
    public List<SpawnedTreeData> trees = new List<SpawnedTreeData>();
    public List<SpawnedBushData> bushes = new List<SpawnedBushData>();
}