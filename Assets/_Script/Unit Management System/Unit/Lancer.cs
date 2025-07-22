using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Lancer : Unit
{
    [Header("Lancer Specific")]
    public float chargeSpeed = 8f;
    public float chargeDuration = 2f;
    public float chargeCooldown = 10f;
    private float nextChargeTime;
    private bool isCharging;

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Lancer;
    }

    public override void UseSpecialAbility()
    {
        // Lao về phía trước
        if (Time.time >= nextChargeTime && !isCharging)
        {
            StartCoroutine(ChargeAttack());
        }
    }

    private System.Collections.IEnumerator ChargeAttack()
    {
        isCharging = true;
        float originalSpeed = moveSpeed;
        moveSpeed = chargeSpeed;

        Debug.Log($"{unitName} đang lao tới!");

        yield return new WaitForSeconds(chargeDuration);

        moveSpeed = originalSpeed;
        isCharging = false;
        nextChargeTime = Time.time + chargeCooldown;

        Debug.Log($"{unitName} đã hoàn thành đòn tấn công!");
    }
}
