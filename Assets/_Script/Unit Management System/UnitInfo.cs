using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UnitInfo
{
    public string unitName;
    public UnitType unitType;
    public UnitState currentState;
    public float health;
    public float maxHealth;
    public Vector3 position;
}