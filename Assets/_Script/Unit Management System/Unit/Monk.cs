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
        unitName = "Monk";
    }

    public override void UseSpecialAbility()
    {
        // Hồi máu cho đồng minh xung quanh
        if (Time.time >= nextHealTime)
        {
            Collider2D[] nearbyUnits = Physics2D.OverlapCircleAll(transform.position, healRange);

            foreach (var collider in nearbyUnits)
            {
                Unit unit = collider.GetComponent<Unit>();
                if (unit != null && unit != this)
                {
                    unit.Heal(healAmount);
                }
            }

            nextHealTime = Time.time + healCooldown;
            Debug.Log($"{unitName} đã hồi máu cho đồng minh!");
        }
    }
}