using UnityEngine;

namespace _Script.ScriptableObjectScript
{
    [CreateAssetMenu(fileName = "NewBuilderStats", menuName = "Unit/Special/Builder Stats Data")]
    public class BuilderStatsSO: UnitStatsSO
    {
        [Header("Builder Specific (Base)")]
        public float baseWorkRate = 10f;

        [Header("Builder Specific (Growth)")]
        public float workRatePerLevel = 2f;
    }
}