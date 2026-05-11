using System.Collections.Generic;
using UnityEngine;

namespace AutoChess
{
    public class GameHUD : MonoBehaviour
    {
        public PlayerEconomy economy;
        public Shop shop;
        public RoundManager rounds;
        public BoardGrid grid;
        public CombatManager combat;
        public Inspector inspector;
        public Camera worldCamera;

        static Texture2D whiteTex;

        class FloatingText
        {
            public Vector3 worldPos;
            public string text;
            public Color color;
            public float age;
            public float scale;
        }

        readonly List<FloatingText> floaters = new();
        const float FloaterLifetime = 0.8f;
        const float FloaterRiseSpeed = 1.2f;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        void OnEnable()
        {
            Unit.Damaged  += HandleDamaged;
            Unit.Died     += HandleDied;
            Unit.Upgraded += HandleUpgraded;
        }

        void OnDisable()
        {
            Unit.Damaged  -= HandleDamaged;
            Unit.Died     -= HandleDied;
            Unit.Upgraded -= HandleUpgraded;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                floaters[i].age += dt;
                if (floaters[i].age >= FloaterLifetime) floaters.RemoveAt(i);
            }
        }

        void HandleDamaged(Unit u, float dmg)
        {
            if (u == null) return;
            int rounded = Mathf.Max(1, Mathf.RoundToInt(dmg));
            Color c = u.team == Team.Player
                ? new Color(1f, 0.45f, 0.35f)
                : new Color(1f, 0.95f, 0.45f);
            float scale = Mathf.Clamp(0.9f + dmg * 0.01f, 0.9f, 1.6f);
            SpawnFloater(u.transform.position, $"-{rounded}", c, scale);
            if (rounded >= 30) CameraShake.Trigger(0.06f);
        }

        void HandleDied(Unit u)
        {
            CameraShake.Trigger(0.15f);
        }

        void HandleUpgraded(Unit u)
        {
            if (u == null) return;
            SpawnFloater(u.transform.position, $"T{u.tier}!",
                         new Color(1f, 0.85f, 0.2f), 1.5f);
            CameraShake.Trigger(0.08f);
        }

        void SpawnFloater(Vector3 pos, string text, Color color, float scale = 1f)
        {
            floaters.Add(new FloatingText
            {
                worldPos = pos,
                text = text,
                color = color,
                age = 0f,
                scale = scale,
            });
        }

        void OnGUI()
        {
            if (economy == null || shop == null || rounds == null || grid == null)
            {
                DrawConfigError();
                return;
            }

            DrawStatusPanel();
            DrawSynergyPanel();
            DrawShopBar();
            DrawTierDots();
            DrawHealthBars();
            DrawFloaters();
            DrawInspectorPanel();
            DrawGameOverPanel();
        }

        void DrawConfigError()
        {
            GUILayout.BeginArea(new Rect(10, 10, 480, 200), GUI.skin.box);
            GUILayout.Label("GameHUD: missing references.");
            if (economy == null) GUILayout.Label("  - PlayerEconomy");
            if (shop == null)    GUILayout.Label("  - Shop");
            if (rounds == null)  GUILayout.Label("  - RoundManager");
            if (grid == null)    GUILayout.Label("  - BoardGrid");
            GUILayout.Label("Drag the matching GameObjects into each empty field.");
            GUILayout.EndArea();
        }

        void DrawStatusPanel()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, 200), GUI.skin.box);
            GUILayout.Label($"Round: {rounds.round}     Phase: {rounds.Phase}");

            var prevColor = GUI.color;
            GUI.color = economy.hp <= 5 ? new Color(1f, 0.4f, 0.4f) : Color.white;
            GUILayout.Label($"HP: {economy.hp}/{economy.maxHP}");
            GUI.color = prevColor;

            GUILayout.Label($"Gold: {economy.gold}     Level: {economy.level}/{economy.MaxLevel}");
            GUILayout.Label($"Board: {grid.CountPlayerCombatUnits()}/{economy.level}");
            GUILayout.Label($"Interest at round end: +{economy.Interest}");

            if (rounds.LastReward > 0 || rounds.LastDamageTaken > 0)
            {
                string outcome = rounds.LastWon ? "WIN" : "LOSS";
                string line = $"Last: {outcome}, +{rounds.LastReward}g  " +
                              $"(base {rounds.LastBase} + win {rounds.LastWin} + " +
                              $"kills {rounds.LastKills} + interest {rounds.LastInterest})";
                if (rounds.LastDamageTaken > 0)
                    line += $"  -{rounds.LastDamageTaken} HP";
                GUILayout.Label(line);
            }

            GUILayout.Label("Right-click a unit to inspect / sell.");
            GUILayout.EndArea();
        }

        void DrawSynergyPanel()
        {
            var s = SynergyEngine.ComputeFromBoard(grid);
            GUILayout.BeginArea(new Rect(380, 10, 380, 130), GUI.skin.box);
            GUILayout.Label("Synergies (board only)");
            DrawTraitLine("Warrior", s.warriorCount, s.WarriorTier, "+20% HP",   "+50% HP");
            DrawTraitLine("Mage",    s.mageCount,    s.MageTier,    "+25% DMG",  "+60% DMG");
            DrawTraitLine("Human",   s.humanCount,   s.HumanTier,   "+15% ASPD", "+35% ASPD");
            DrawTraitLine("Beast",   s.beastCount,   s.BeastTier,   "+20% DMG",  "+45% DMG");
            GUILayout.EndArea();
        }

        void DrawTraitLine(string name, int count, int tier, string t1, string t2)
        {
            string mark = tier > 0 ? "*" : "-";
            string body = tier switch
            {
                2 => t2,
                1 => $"{t1}   ->   3: {t2}",
                _ => $"2: {t1},  3: {t2}",
            };
            string text = $"{mark} {name} {count}/3   {body}";

            var prev = GUI.color;
            GUI.color = tier > 0 ? new Color(0.7f, 1f, 0.7f) : Color.white;
            GUILayout.Label(text);
            GUI.color = prev;
        }

        void DrawShopBar()
        {
            const float barH = 130f;
            const float pad  = 10f;
            float y = Screen.height - barH - pad;

            GUILayout.BeginArea(new Rect(pad, y, Screen.width - 2 * pad, barH), GUI.skin.box);
            GUILayout.BeginHorizontal();

            bool prep = rounds.Phase == GamePhase.Prep;

            for (int i = 0; i < shop.slotCount; i++)
                DrawShopSlot(i, prep);

            GUILayout.Space(15);
            DrawActionButtons(prep);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawShopSlot(int i, bool prep)
        {
            var data = shop.currentSlots[i];
            string label;
            if (data == null) label = "(empty)";
            else if (shop.slotPurchased[i]) label = "(sold)";
            else label =
                $"{data.displayName}\n" +
                $"{data.unitClass} / {data.unitType}\n" +
                $"HP {data.maxHealth:0}  DMG {data.attackDamage:0}\n" +
                $"R {data.attackRange}  ASPD {data.attackSpeed:0.#}\n" +
                $"{data.cost}g";

            bool buyable = prep && data != null && !shop.slotPurchased[i] && economy.CanAfford(data.cost);
            GUI.enabled = buyable;
            if (GUILayout.Button(label, GUILayout.Width(140), GUILayout.Height(110)))
                shop.TryBuy(i);
            GUI.enabled = true;
        }

        void DrawActionButtons(bool prep)
        {
            GUILayout.BeginVertical(GUILayout.Width(170));

            GUI.enabled = prep && economy.CanAfford(shop.refreshCost);
            if (GUILayout.Button($"Refresh ({shop.refreshCost}g)", GUILayout.Height(26)))
                shop.TryRefresh();

            GUI.enabled = prep && !economy.IsAtMaxLevel && economy.CanAfford(economy.LevelUpCost);
            string levelLabel = economy.IsAtMaxLevel
                ? "Max Level"
                : $"Level Up ({economy.LevelUpCost}g)";
            if (GUILayout.Button(levelLabel, GUILayout.Height(26)))
                economy.TryBuyLevel();

            GUI.enabled = prep;
            string mainLabel = rounds.Phase switch
            {
                GamePhase.Prep     => "Start Battle",
                GamePhase.Combat   => "Battling...",
                GamePhase.Result   => "Resolving...",
                GamePhase.GameOver => "Game Over",
                _ => "Start Battle",
            };
            if (GUILayout.Button(mainLabel, GUILayout.Height(26)))
                rounds.StartBattle();
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        void DrawInspectorPanel()
        {
            if (inspector == null || !inspector.HasUnit) return;
            var u = inspector.Current;
            if (u == null) { inspector.Clear(); return; }
            if (u.data == null) { inspector.Clear(); return; }

            const float w = 290f;
            const float h = 360f;
            float x = Screen.width - w - 10f;
            float y = 10f;

            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);

            GUILayout.Label($"{u.data.displayName}   (T{u.tier})");
            GUILayout.Label($"Team: {u.team}");
            GUILayout.Label($"Class: {u.data.unitClass}");
            GUILayout.Label($"Type:  {u.data.unitType}");
            GUILayout.Space(4);
            GUILayout.Label($"HP:        {u.CurrentHealth:0} / {u.MaxHealth:0}");
            GUILayout.Label($"Damage:    {u.AttackDamage:0.#}");
            GUILayout.Label($"Range:     {u.data.attackRange} tile(s)");
            GUILayout.Label($"Atk speed: {u.data.attackSpeed:0.##}/s");
            GUILayout.Label($"Move:      {u.data.moveSpeed:0.#} tiles/s");
            GUILayout.Space(6);

            bool prep = rounds.Phase == GamePhase.Prep;

            if (u.team == Team.Player && shop != null)
            {
                GUI.enabled = prep;
                if (GUILayout.Button($"Sell  ({u.SellValue}g)", GUILayout.Height(32)))
                {
                    shop.Sell(u);
                    inspector.Clear();
                    GUILayout.EndArea();
                    return;
                }
                GUI.enabled = true;
                if (!prep) GUILayout.Label("(can only sell during Prep)");
            }

            if (GUILayout.Button("Close", GUILayout.Height(24)))
                inspector.Clear();
            GUILayout.EndArea();
        }

        void DrawGameOverPanel()
        {
            if (rounds.Phase != GamePhase.GameOver) return;

            const float w = 420f;
            const float h = 220f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);
            GUILayout.Label("GAME OVER");
            GUILayout.Label($"You reached round {rounds.round}.");
            GUILayout.Label($"Final level: {economy.level}     Final gold: {economy.gold}");
            GUILayout.Space(20);
            if (GUILayout.Button("Restart", GUILayout.Height(40)))
                rounds.Restart();
            GUILayout.EndArea();
        }

        void DrawTierDots()
        {
            if (worldCamera == null) return;
            foreach (var t in grid.AllTiles())
            {
                if (t.occupant == null) continue;
                var u = t.occupant;
                if (u.tier <= 1) continue;
                if (!u.IsAlive && combat != null && combat.InCombat) continue;
                DrawTierDotsFor(u);
            }
        }

        void DrawTierDotsFor(Unit u)
        {
            Vector3 sp = worldCamera.WorldToScreenPoint(u.transform.position);
            if (sp.z < 0f) return;

            int count = u.tier;
            float size = 7f;
            float spacing = 11f;
            float totalW = (count - 1) * spacing + size;
            float x = sp.x - totalW * 0.5f;
            float y = Screen.height - sp.y - 30f;

            Color border = Color.black;
            Color fill = new Color(1f, 0.85f, 0.2f);

            for (int i = 0; i < count; i++)
            {
                float dx = x + i * spacing;
                DrawSolid(new Rect(dx - 1f, y - 1f, size + 2f, size + 2f), border);
                DrawSolid(new Rect(dx, y, size, size), fill);
            }
        }

        void DrawHealthBars()
        {
            if (combat == null || !combat.InCombat || worldCamera == null) return;
            DrawHealthBarsFor(combat.PlayerUnits);
            DrawHealthBarsFor(combat.EnemyUnits);
        }

        void DrawHealthBarsFor(IReadOnlyList<Unit> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || !u.IsAlive) continue;
                DrawHealthBar(u);
            }
        }

        void DrawHealthBar(Unit u)
        {
            Vector3 sp = worldCamera.WorldToScreenPoint(u.transform.position);
            if (sp.z < 0f) return;
            float barW = 50f;
            float barH = 6f;
            float x = sp.x - barW * 0.5f;
            float y = Screen.height - sp.y - 38f;

            float pct = Mathf.Clamp01(u.CurrentHealth / Mathf.Max(1f, u.MaxHealth));

            DrawSolid(new Rect(x - 1f, y - 1f, barW + 2f, barH + 2f), Color.black);
            DrawSolid(new Rect(x, y, barW, barH), new Color(0.25f, 0.25f, 0.25f));
            Color fill = u.team == Team.Player ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            DrawSolid(new Rect(x, y, barW * pct, barH), fill);
        }

        void DrawFloaters()
        {
            if (worldCamera == null || floaters.Count == 0) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };

            for (int i = 0; i < floaters.Count; i++)
            {
                var ft = floaters[i];
                float t = ft.age / FloaterLifetime;
                float alpha = 1f - t;
                Vector3 wp = ft.worldPos + new Vector3(0f, ft.age * FloaterRiseSpeed, 0f);
                Vector3 sp = worldCamera.WorldToScreenPoint(wp);
                if (sp.z < 0f) continue;

                int fontSize = Mathf.RoundToInt(14f * ft.scale);
                style.fontSize = fontSize;

                var prev = GUI.color;
                GUI.color = new Color(ft.color.r, ft.color.g, ft.color.b, alpha);
                Rect r = new Rect(sp.x - 40f, Screen.height - sp.y - 60f, 80f, 24f);
                GUI.Label(r, ft.text, style);
                GUI.color = prev;
            }
        }

        static void DrawSolid(Rect rect, Color color)
        {
            if (whiteTex == null) whiteTex = Texture2D.whiteTexture;
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = prev;
        }
    }
}
