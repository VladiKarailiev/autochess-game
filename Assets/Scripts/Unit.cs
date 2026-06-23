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

        [Header("Ability")]
        public AbilityKind ability = AbilityKind.None;
        [Min(0.5f)] public float abilityCooldown = 5f;
        public float abilityPower = 20f;     // damage / heal / shield amount (see ability notes)
        public float abilityDuration = 3f;   // seconds for DoT / HoT / buff / shield
        public float abilityRadius = 1.5f;   // AoE radius for Fireball

        // Runtime only: if serialized, Unity defaults it to a non-null item.
        [System.NonSerialized] public ItemDef equippedItem;

        [Header("Visual children")]
        public UnitOverlay overlay;

        Tile currentTile;
        SpriteRenderer spriteRenderer;

        float currentHealth;
        Unit  currentTarget;
        float attackCooldownRemaining;

        float abilityCdRemaining;
        float tempAspdMult = 1f;   // temporary attack-speed buff (Bloodlust)
        float buffRemaining;
        float bonusNextHit;        // Maul
        float shieldRemaining;     // ShieldWall absorb pool
        float shieldTimer;
        float poisonDps;           // ToxicShot
        float poisonRemaining;
        float healPerSecond;       // Regrowth
        float hotRemaining;

        float hpMultiplier = 1f;
        float damageMultiplier = 1f;
        float attackSpeedMultiplier = 1f;
        float synergyRangeBonus;

        Color prefabBaseColor = Color.white;
        Color baseColor = Color.white;
        float hitFlashTimer;

        public static event System.Action<Unit, float> Damaged;
        public static event System.Action<Unit> Died;
        public static event System.Action<Unit> Upgraded;
        public static event System.Action<Unit> AbilityCast;

        public Tile  CurrentTile  => currentTile;
        public bool  IsAlive      => currentHealth > 0f;
        public float CurrentHealth=> currentHealth;

        public float MaxHealth      => maxHealth * TierStatMultiplier(tier) * hpMultiplier + ItemAmountFor(ItemStat.Health);
        public float AttackDamage   => attackDamage * TierStatMultiplier(tier) * damageMultiplier + ItemAmountFor(ItemStat.Damage);
        public float AttackInterval => 1f / Mathf.Max(0.01f, (attackSpeed + ItemAmountFor(ItemStat.AttackSpeed)) * attackSpeedMultiplier * tempAspdMult);
        public float EffectiveRange => attackRange + synergyRangeBonus + ItemAmountFor(ItemStat.Range);

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

        float ItemAmountFor(ItemStat stat)
        {
            return (equippedItem != null && equippedItem.stat == stat) ? equippedItem.amount : 0f;
        }

        public bool CanEquip => equippedItem == null;

        public void Equip(ItemDef item)
        {
            if (item == null || equippedItem != null) return;
            equippedItem = item;
        }

        public void RemoveItem()
        {
            equippedItem = null;
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

        public void ApplyCombatBuffs(float hpMult, float dmgMult, float aspdMult, float rangeAdd)
        {
            hpMultiplier = hpMult;
            damageMultiplier = dmgMult;
            attackSpeedMultiplier = aspdMult;
            synergyRangeBonus = rangeAdd;
        }

        public void ClearCombatBuffs()
        {
            hpMultiplier = 1f;
            damageMultiplier = 1f;
            attackSpeedMultiplier = 1f;
            synergyRangeBonus = 0f;
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
            if (spriteRenderer == null || !spriteRenderer.enabled) return;

            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                if (hitFlashTimer <= 0f)
                    spriteRenderer.color = StatusColor();
            }
            else
            {
                spriteRenderer.color = StatusColor();
            }
        }

        // Tints the unit while an ability effect is active, so the player can see
        // what's going on (e.g. a shielded unit glows blue while it absorbs hits).
        Color StatusColor()
        {
            if (poisonRemaining > 0f) return Color.Lerp(baseColor, new Color(0.45f, 0.9f, 0.25f), 0.6f);  // poisoned
            if (shieldRemaining > 0f) return Color.Lerp(baseColor, new Color(0.45f, 0.7f, 1f), 0.6f);      // shielded
            if (buffRemaining  > 0f)  return Color.Lerp(baseColor, new Color(1f, 0.6f, 0.2f), 0.6f);       // frenzied
            if (hotRemaining   > 0f)  return Color.Lerp(baseColor, new Color(0.55f, 1f, 0.6f), 0.5f);      // regrowing
            return baseColor;
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
            ResetAbilityState();
            SetRendererVisible(true);
            ShowHpBar(true);
            UpdateOverlayHp();
        }

        void ResetAbilityState()
        {
            // First cast happens soon but at a random offset per unit, so casts
            // don't all fire on the same tick. Later casts use the real cooldown.
            abilityCdRemaining = Random.Range(0.5f, 2f);
            tempAspdMult = 1f;
            buffRemaining = 0f;
            bonusNextHit = 0f;
            shieldRemaining = 0f;
            shieldTimer = 0f;
            poisonDps = 0f;
            poisonRemaining = 0f;
            healPerSecond = 0f;
            hotRemaining = 0f;
        }

        public void Tick(float dt, IReadOnlyList<Unit> all)
        {
            if (!IsAlive) return;

            TickStatus(dt);
            if (!IsAlive) return;   // poison may have finished us off

            if (currentTarget == null || !currentTarget.IsAlive)
                currentTarget = FindClosestEnemy(all);

            TickAbility(dt, all);

            if (currentTarget == null) return;

            if (attackCooldownRemaining > 0f)
                attackCooldownRemaining -= dt;

            float dist = Vector2.Distance(transform.position, currentTarget.transform.position);
            float rangeWorld = EffectiveRange;

            if (dist <= rangeWorld)
            {
                if (attackCooldownRemaining <= 0f)
                {
                    float dmg = AttackDamage + bonusNextHit;
                    bonusNextHit = 0f;
                    currentTarget.TakeDamage(dmg);
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

        void TickStatus(float dt)
        {
            if (poisonRemaining > 0f)
            {
                poisonRemaining -= dt;
                ApplyDotDamage(poisonDps * dt);   // poison bypasses shields
            }
            if (hotRemaining > 0f)
            {
                hotRemaining -= dt;
                Heal(healPerSecond * dt);
            }
            if (buffRemaining > 0f)
            {
                buffRemaining -= dt;
                if (buffRemaining <= 0f) tempAspdMult = 1f;
            }
            if (shieldTimer > 0f)
            {
                shieldTimer -= dt;
                if (shieldTimer <= 0f) shieldRemaining = 0f;
            }
        }

        void TickAbility(float dt, IReadOnlyList<Unit> all)
        {
            if (ability == AbilityKind.None) return;
            abilityCdRemaining -= dt;
            if (abilityCdRemaining > 0f) return;
            abilityCdRemaining = abilityCooldown;
            CastAbility(all);
        }

        void CastAbility(IReadOnlyList<Unit> all)
        {
            switch (ability)
            {
                case AbilityKind.ShieldWall:
                    shieldRemaining = abilityPower;
                    shieldTimer = abilityDuration;
                    break;
                case AbilityKind.Bloodlust:
                    tempAspdMult = 1f + abilityPower / 100f;   // power = % attack speed
                    buffRemaining = abilityDuration;
                    break;
                case AbilityKind.Maul:
                    bonusNextHit = abilityPower;
                    break;
                case AbilityKind.Fireball:
                    if (currentTarget != null)
                        DamageEnemiesNear(all, currentTarget.transform.position, abilityRadius, abilityPower);
                    break;
                case AbilityKind.Heal:
                    HealLowestAlly(all, abilityPower);
                    break;
                case AbilityKind.Regrowth:
                    healPerSecond = abilityPower / Mathf.Max(0.1f, abilityDuration);
                    hotRemaining = abilityDuration;
                    break;
                case AbilityKind.ToxicShot:
                    if (currentTarget != null)
                        currentTarget.ApplyPoison(abilityPower / Mathf.Max(0.1f, abilityDuration), abilityDuration);
                    break;
                case AbilityKind.PiercingShot:
                    if (currentTarget != null) currentTarget.TakeDamage(abilityPower);
                    break;
                case AbilityKind.Volley:
                    DamageNearestEnemies(all, 3, abilityPower);
                    break;
            }
            AbilityCast?.Invoke(this);
        }

        void DamageEnemiesNear(IReadOnlyList<Unit> all, Vector3 center, float radius, float dmg)
        {
            float r2 = radius * radius;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (u == null || u.team == team || !u.IsAlive) continue;
                if (((Vector2)u.transform.position - (Vector2)center).sqrMagnitude <= r2)
                    u.TakeDamage(dmg);
            }
        }

        void DamageNearestEnemies(IReadOnlyList<Unit> all, int maxTargets, float dmg)
        {
            var hit = new HashSet<Unit>();
            Vector2 me = transform.position;
            while (hit.Count < maxTargets)
            {
                Unit best = null;
                float bestSqr = float.MaxValue;
                for (int i = 0; i < all.Count; i++)
                {
                    var u = all[i];
                    if (u == null || u.team == team || !u.IsAlive || hit.Contains(u)) continue;
                    float sqr = ((Vector2)u.transform.position - me).sqrMagnitude;
                    if (sqr < bestSqr) { bestSqr = sqr; best = u; }
                }
                if (best == null) break;
                best.TakeDamage(dmg);
                hit.Add(best);
            }
        }

        void HealLowestAlly(IReadOnlyList<Unit> all, float amount)
        {
            Unit lowest = null;
            float lowestPct = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (u == null || u.team != team || !u.IsAlive) continue;
                float pct = u.currentHealth / Mathf.Max(1f, u.MaxHealth);
                if (pct < lowestPct) { lowestPct = pct; lowest = u; }
            }
            if (lowest != null) lowest.Heal(amount);
        }

        public void ApplyPoison(float dps, float duration)
        {
            poisonDps = dps;
            poisonRemaining = duration;
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
            UpdateOverlayHp();
        }

        void ApplyDotDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            currentHealth -= amount;
            UpdateOverlayHp();
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Died?.Invoke(this);
                SetRendererVisible(false);
                ShowHpBar(false);
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

            if (shieldRemaining > 0f)
            {
                float absorbed = Mathf.Min(shieldRemaining, damage);
                shieldRemaining -= absorbed;
                damage -= absorbed;
                if (absorbed > 0f) FlashColor(new Color(0.55f, 0.8f, 1f), 0.1f);   // shield ping
            }
            if (damage <= 0f) { UpdateOverlayHp(); return; }

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
            ResetAbilityState();
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
