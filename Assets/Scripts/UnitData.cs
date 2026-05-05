using UnityEngine;

namespace AutoChess
{
    [CreateAssetMenu(fileName = "NewUnit", menuName = "AutoChess/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Unnamed";
        public UnitClass unitClass;
        public UnitType unitType;

        [Header("Economy")]
        [Min(1)] public int cost = 1;

        [Header("Combat (1-star base stats)")]
        [Min(1f)] public float maxHealth = 100f;
        [Min(1f)] public float attackDamage = 10f;
        [Min(1)]  public int attackRange = 1;
        [Min(0.1f)] public float attackSpeed = 1f;
        [Min(0.1f)] public float moveSpeed = 2f;

        [Header("Visual (placeholder)")]
        public Color displayColor = Color.white;
    }
}
