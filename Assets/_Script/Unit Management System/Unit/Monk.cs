using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monk : Unit
{
    [Header("Priest Specific")]
    public float healAmount = 20f;
    public float healRange = 3f;
    public float healCooldown = 5f;
    private float nextHealTime;

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Monk;
    }

    public override void UseSpecialAbility()
    {
    }

}