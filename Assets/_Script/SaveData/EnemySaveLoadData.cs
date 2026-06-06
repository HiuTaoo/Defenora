using System;
using UnityEngine;

[Serializable]
public class EnemySaveLoadData
{
    public string id;
    public string unitName;
    public UnitType unitType;
    public int level;
    public int layerIndex;
    public float currentHealth;
    public Vector3 position;
    public string assignedBuilding;
    public string spawnPointId;
}