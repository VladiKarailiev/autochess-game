using System.Collections.Generic;
using UnityEngine;

namespace AutoChess
{
    public class Unit : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Unique key used for upgrades and synergies.")]
        public string displayName = "Unnamed";
        public UnitClass unitClass;
        public UnitType unitType;
        [Tooltip("Short role text shown in the shop and inspector.")]
        public string roleDescription = "Balanced fighter";

        [Header("Economy")]
        [Min(1)] public int cost = 1;

        [Header("Combat (tier-1 base stats)")]
        [Min(1f)] public float maxHealth = 100f;
        [Min(1f)] public float attackDamage = 10f;
        [Min(1)]  public int   attackRange = 1;
        [Min(0.1f)] public float attackSpeed = 1f;
        [Min(0.1f)] public float moveSpeed = 2f;

        [Header("Runtime state")]
        public Team team = Team.Player;
        [Range(1, 3)] public int tier = 1;
        [Range(0.1f, 1f)] public float visualScale = 0.6f;

        [Header("Visual children")]
        public UnitOverlay overlay;

        Tile currentTile;
        SpriteRenderer spriteRenderer;

        float currentHealth;
        Unit  currentTarget;
        float attackCooldownRemaining;

        float hpMultiplier = 1f;
        float damageMultiplier = 1f;
        float attackSpeedMultiplier = 1f;

        Color prefabBaseColor = Color.white;
        Color baseColor = Color.white;
        float hitFlashTimer;

        public static event System.Action<Unit, float> Damaged;
        public static event System.Action<Unit> Died;
        public static event System.Action<Unit> Upgraded;

        public Tile  CurrentTile  => currentTile;
        public bool  IsAlive      => currentHealth > 0f;
        public float CurrentHealth=> currentHealth;

        public float MaxHealth      => maxHealth * TierStatMultiplier(tier) * hpMultiplier;
        public float AttackDamage   => attackDamage * TierStatMultiplier(tier) * damageMultiplier;
        public float AttackInterval => 1f / Mathf.Max(0.01f, attackSpeed * attackSpeedMultiplier);

        public int SellValue
        {
            get
            {
                int mult = tier switch { 2 => 3, 3 => 9, _ => 1 };
                return cost * mult;
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

            prefabBaseColor = spriteRenderer.color;
            spriteRenderer.sortingOrder = 10;
            ApplyVisual();
        }

        public void Initialize(Team newTeam)
        {
            team = newTeam;
            tier = 1;
            if (spriteRenderer != null) ApplyVisual();
        }

        public void Upgrade()
        {
            if (tier >= 3) return;
            tier++;
            if (spriteRenderer != null) ApplyVisual();
            currentHealth = MaxHealth;
            FlashColor(new Color(1f, 0.9f, 0.3f), 0.25f);
            Upgraded?.Invoke(this);
        }

        public void ApplyCombatBuffs(float hpMult, float dmgMult, float aspdMult)
        {
            hpMultiplier = hpMult;
            damageMultiplier = dmgMult;
            attackSpeedMultiplier = aspdMult;
        }

        public void ClearCombatBuffs()
        {
            hpMultiplier = 1f;
            damageMultiplier = 1f;
            attackSpeedMultiplier = 1f;
        }

        void ApplyVisual()
        {
            Color final = team == Team.Enemy
                ? Color.Lerp(prefabBaseColor, new Color(0.95f, 0.15f, 0.15f), 0.55f)
                : prefabBaseColor;
            baseColor = final;

            if (hitFlashTimer <= 0f) spriteRenderer.color = final;

            float tierScale = tier switch { 2 => 1.15f, 3 => 1.35f, _ => 1f };
            transform.localScale = Vector3.one * visualScale * tierScale;
            gameObject.name = $"Unit_{displayName}_T{tier}_{team}";

            UpdateOverlayTier();
            UpdateOverlayHp();
        }

        void UpdateOverlayTier()
        {
            if (overlay == null || overlay.tierDots == null) return;
            int dotsToShow = tier > 1 ? tier : 0;
            for (int i = 0; i < overlay.tierDots.Length; i++)
            {
                if (overlay.tierDots[i] != null)
                    overlay.tierDots[i].gameObject.SetActive(i < dotsToShow);
            }
        }

        void UpdateOverlayHp()
        {
            if (overlay == null || overlay.hpBarFill == null) return;
            float pct = Mathf.Clamp01(currentHealth / Mathf.Max(1f, MaxHealth));
            overlay.hpBarFill.fillAmount = pct;
            overlay.hpBarFill.color = team == Team.Player
                ? new Color(0.3f, 0.85f, 0.3f)
                : new Color(0.9f, 0.3f, 0.3f);
        }

        void ShowHpBar(bool show)
        {
            if (overlay != null && overlay.hpBarRoot != null)
                overlay.hpBarRoot.SetActive(show);
        }

        void SetRendererVisible(bool visible)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;
        }

        void FlashColor(Color flash, float duration)
        {
            if (spriteRenderer == null) return;
            spriteRenderer.color = flash;
            hitFlashTimer = duration;
        }

        void Update()
        {
            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                if (hitFlashTimer <= 0f && spriteRenderer != null && spriteRenderer.enabled)
                    spriteRenderer.color = baseColor;
            }
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
            SetRendererVisible(true);
            ShowHpBar(true);
            UpdateOverlayHp();
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
            float rangeWorld = attackRange;

            if (dist <= rangeWorld)
            {
                if (attackCooldownRemaining <= 0f)
                {
                    currentTarget.TakeDamage(AttackDamage);
                    attackCooldownRemaining = AttackInterval;
                }
            }
            else
            {
                Vector2 dir = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
                float maxStep = moveSpeed * dt;
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
            Damaged?.Invoke(this, damage);
            FlashColor(Color.white, 0.08f);
            UpdateOverlayHp();
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Died?.Invoke(this);
                SetRendererVisible(false);
                ShowHpBar(false);
            }
        }

        public void RestoreToTile(Tile home)
        {
            currentHealth = MaxHealth;
            currentTarget = null;
            attackCooldownRemaining = 0f;
            hitFlashTimer = 0f;
            if (spriteRenderer != null)
            {
                SetRendererVisible(true);
                spriteRenderer.color = baseColor;
            }
            ShowHpBar(false);
            UpdateOverlayHp();
            if (home != null) PlaceOnTile(home);
        }
    }
}
