using System;
using System.Collections.Generic;

[Serializable]
public class SpawnerCycleSaveData
{
    public int bushDayTracker;
    public List<ChoppedTreeSaveEntry> choppedTrees = new();
    public List<int> deadAnimalLayers = new();

    public List<int> harvestedBushLayers = new();
}