using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder : Unit
{
    [Header("Builder Specific")]
    public float buildSpeed = 1f;
    public GameObject[] buildablePrefabs;
    private bool isBuilding;

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Builder;
    }

    public override void UseSpecialAbility()
    {
        // Xây dựng công trình
        if (!isBuilding && buildablePrefabs.Length > 0)
        {
            StartCoroutine(BuildStructure());
        }
    }

    private System.Collections.IEnumerator BuildStructure()
    {
        isBuilding = true;
        currentState = UnitState.Working;

        Debug.Log($"{unitName} bắt đầu xây dựng!");

        yield return new WaitForSeconds(3f / buildSpeed);

        // Tạo công trình ngẫu nhiên
        GameObject prefab = buildablePrefabs[Random.Range(0, buildablePrefabs.Length)];
        Instantiate(prefab, transform.position + Vector3.up, Quaternion.identity);

        isBuilding = false;
        currentState = UnitState.Idle;

        Debug.Log($"{unitName} đã hoàn thành xây dựng!");
    }
}