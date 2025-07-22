using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer : Unit
{
    [Header("Archer Specific")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    private float nextFireTime;

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Archer;
    }

    public override void UseSpecialAbility()
    {
        // Bắn tên
        if (Time.time >= nextFireTime && arrowPrefab != null && firePoint != null)
        {
            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
            nextFireTime = Time.time + 1f / fireRate;

            Debug.Log($"{unitName} đã bắn tên!");
        }
    }
}