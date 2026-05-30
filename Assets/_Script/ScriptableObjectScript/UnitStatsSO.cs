using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitStats", menuName = "Unit/Unit Stats Data")]
public class UnitStatsSO : ScriptableObject
{
    [Header("Basic Info")] public string unitName;

    public Sprite unitIcon;
    public int maxLevel = 10;

    [Header("Base Stats (Level 1)")] public float baseMaxHealth;

    public float baseAttackDamage;
    public float baseViewDistance;

    [Header("Growth Per Level")] public float healthPerLevel;

    public float attackDamagePerLevel;
    public float viewDistancePerLevel;

    [Header("Attack Cooldown")] public float attackCooldown;
}