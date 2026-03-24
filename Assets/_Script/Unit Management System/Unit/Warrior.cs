using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warrior : Unit
{
    [Header("Warrior Specific")]
    public float chargeSpeed = 8f;
    public float chargeDuration = 2f;
    public float chargeCooldown = 10f;
    private float nextChargeTime;
    private bool isCharging;

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Warrior;
    }

    public override void UseSpecialAbility()
    {
     
    }

}
