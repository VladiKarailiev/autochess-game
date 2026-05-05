using System.Collections.Generic;
using UnityEngine;

namespace AutoChess
{
    public class Unit : MonoBehaviour
    {
        public UnitData data;
        public Team team = Team.Player;
        [Range(1, 3)] public int tier = 1;
        [Range(0.1f, 1f)] public float visualScale = 0.6f;

        Tile currentTile;
        SpriteRenderer spriteRenderer;

        float currentHealth;
        Unit currentTarget;
        float attackCooldownRemaining;

        float hpMultiplier = 1f;
        float damageMultiplier = 1f;

        public Tile CurrentTile => currentTile;
        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;

        public float MaxHealth =>
            data != null ? data.maxHealth * TierStatMultiplier(tier) * hpMultiplier : 1f;

        public float AttackDamage =>
            data != null ? data.attackDamage * TierStatMultiplier(tier) * damageMultiplier : 0f;

        public int SellValue
        {
            get
            {
                if (data == null) return 0;
                int mult = tier switch { 2 => 3, 3 => 9, _ => 1 };
                return data.cost * mult;
            }
        }

        static float TierStatMultiplier(int t)
        {
            switch (t)
            {
                case 2: return 1.8f;
                case 3: return 3.0f;
                default: return 1f;
            }
        }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            spriteRenderer.sprite = Sprites.GetCircle();
            spriteRenderer.sortingOrder = 10;
            ApplyData();
        }

        public void Initialize(UnitData newData, Team newTeam)
        {
            data = newData;
            team = newTeam;
            tier = 1;
            if (spriteRenderer != null) ApplyData();
        }

        public void SetData(UnitData newData)
        {
            data = newData;
            if (spriteRenderer != null) ApplyData();
        }

        public void Upgrade()
        {
            if (tier >= 3 || data == null) return;
            tier++;
            if (spriteRenderer != null) ApplyData();
            currentHealth = MaxHealth;
        }

        void ApplyData()
        {
            if (data == null) return;

            Color baseColor = data.displayColor;
            Color final = team == Team.Enemy
                ? Color.Lerp(baseColor, new Color(0.95f, 0.15f, 0.15f), 0.55f)
                : baseColor;
            spriteRenderer.color = final;

            float tierScale = tier switch { 2 => 1.15f, 3 => 1.35f, _ => 1f };
            transform.localScale = Vector3.one * visualScale * tierScale;
            gameObject.name = $"Unit_{data.displayName}_T{tier}_{team}";
        }

        public void PlaceOnTile(Tile tile)
        {
            if (tile == null) return;

            if (currentTile != null && currentTile.occupant == this)
                currentTile.occupant = null;

            currentTile = tile;
            tile.occupant = this;
            transform.position = tile.transform.position;
        }

        public void OnCombatStart()
        {
            currentHealth = MaxHealth;
            currentTarget = null;
            attackCooldownRemaining = 0f;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }

        public void Tick(float dt, IReadOnlyList<Unit> all)
        {
            if (!IsAlive) return;

            if (currentTarget == null || !currentTarget.IsAlive)
                currentTarget = FindClosestEnemy(all);
            if (currentTarget == null) return;

            if (attackCooldownRemaining > 0f)
                attackCooldownRemaining -= dt;

            float dist = Vector2.Distance(transform.position, currentTarget.transform.position);
            float rangeWorld = data.attackRange;

            if (dist <= rangeWorld)
            {
                if (attackCooldownRemaining <= 0f)
                {
                    currentTarget.TakeDamage(AttackDamage);
                    attackCooldownRemaining = 1f / Mathf.Max(0.01f, data.attackSpeed);
                }
            }
            else
            {
                Vector2 dir = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
                float maxStep = data.moveSpeed * dt;
                float step = Mathf.Min(maxStep, dist - rangeWorld);
                if (step > 0f)
                    transform.position += (Vector3)(dir * step);
            }
        }

        Unit FindClosestEnemy(IReadOnlyList<Unit> all)
        {
            Unit best = null;
            float bestSqr = float.MaxValue;
            Vector2 me = transform.position;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (u == null || u == this) continue;
                if (u.team == team) continue;
                if (!u.IsAlive) continue;
                float sqr = ((Vector2)u.transform.position - me).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = u; }
            }
            return best;
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                if (spriteRenderer != null) spriteRenderer.enabled = false;
            }
        }

        public void RestoreToTile(Tile home)
        {
            currentHealth = MaxHealth;
            currentTarget = null;
            attackCooldownRemaining = 0f;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (home != null) PlaceOnTile(home);
        }
    }
}
