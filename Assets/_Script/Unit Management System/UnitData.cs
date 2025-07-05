using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UnitData
{
    public string unitName;
    public UnitType unitType;
    public UnitState currentState;
    public int layerIndex;
    public float health;
    public float maxHealth;
    public Vector3 position;
    public string assignedBuilding;
}