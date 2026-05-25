using System;
using UnityEngine;

public class UnitStatsManager : MonoBehaviour
{
    [SerializeField] private UnitStatsSO unitData;
    
    public int currentLevel { get; private set; } = 1;

    public float MaxHealth { get; private set; }
    public float AttackDamage { get; private set; }
    public float ViewDistance { get; private set; }
    
    public event Action OnLevelUp;
    public event Action OnStatsUpdated;

    private void Awake()
    {
        CalculateStats();
    }

    public void CalculateStats()
    {
        if (unitData == null) return;

        int levelMultiplier = currentLevel - 1;

        MaxHealth = unitData.baseMaxHealth + (unitData.healthPerLevel * levelMultiplier);
        AttackDamage = unitData.baseAttackDamage + (unitData.attackDamagePerLevel * levelMultiplier);
        ViewDistance = unitData.baseViewDistance + (unitData.viewDistancePerLevel * levelMultiplier);

        OnStatsUpdated?.Invoke();
    }

    public void LevelUp()
    {
        if (currentLevel >= unitData.maxLevel) return;

        currentLevel++;
        CalculateStats();
        OnLevelUp?.Invoke();
    }

    public bool IsMaxLevelUp()
    {
        return currentLevel >= unitData.maxLevel;
    }

    public void SetLevel(int level)
    {
        if (unitData == null)
            return;

        currentLevel = Mathf.Clamp(level, 1, unitData.maxLevel);
        CalculateStats();
    }

    public UnitStatsSO GetBaseData() => unitData;
}