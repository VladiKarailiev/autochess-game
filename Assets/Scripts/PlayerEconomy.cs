using UnityEngine;

namespace AutoChess
{
    public class PlayerEconomy : MonoBehaviour
    {
        [Header("Starting state")]
        [Min(0)] public int gold = 10;
        [Min(2)] public int level = 2;
        [Min(1)] public int maxHP = 20;

        public int hp { get; private set; }

        public bool IsDead => hp <= 0;

        void Awake()
        {
            hp = maxHP;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            hp = Mathf.Max(0, hp - amount);
        }

        public static readonly int[] LevelUpCosts = { 4, 8, 12, 20, 32 };

        public int MaxLevel => 2 + LevelUpCosts.Length;
        public bool IsAtMaxLevel => level >= MaxLevel;

        public int LevelUpCost
        {
            get
            {
                int idx = level - 2;
                if (idx < 0 || idx >= LevelUpCosts.Length) return -1;
                return LevelUpCosts[idx];
            }
        }

        public int Interest => Mathf.Min(gold / 10, 5);

        public bool CanAfford(int cost) => gold >= cost;

        public bool TrySpend(int cost)
        {
            if (gold < cost) return false;
            gold -= cost;
            return true;
        }

        public void Gain(int amount)
        {
            if (amount <= 0) return;
            gold += amount;
        }

        public bool TryBuyLevel()
        {
            if (IsAtMaxLevel) return false;
            int cost = LevelUpCost;
            if (cost < 0 || !TrySpend(cost)) return false;
            level++;
            return true;
        }
    }
}
