using UnityEngine;

namespace _Script.ScriptableObjectScript
{
    [CreateAssetMenu(fileName = "NewBuilderStats", menuName = "Unit/Special/Barrel Stats Data")]
    public class BarrelStatsSO: UnitStatsSO
    {
        [Header("Barrel Specific (Base)")]
        public float explosionDamage = 10f;

        [Header("Barrel Specific (Growth)")]
        public float explosionDamagePerLevel = 2f;
    }
}