using UnityEngine;

namespace _Script.ScriptableObjectScript
{
    [CreateAssetMenu(fileName = "NewMonkStats", menuName = "Unit/Special/Monk Stats Data")]
    public class MonkStatsSO: UnitStatsSO
    {
        [Header("Builder Specific (Base)")]
        public float baseHealAmount = 30f;
        public float baseHealRange = 3f;

        [Header("Builder Specific (Growth)")]
        public float healAmountPerLevel = 5f;
        public float healRangePerLevel = 0.25f;
    }
}