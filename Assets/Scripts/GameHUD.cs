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
        public Camera worldCamera;

        static Texture2D whiteTex;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        void OnGUI()
        {
            if (economy == null || shop == null || rounds == null || grid == null)
            {
                DrawConfigError();
                return;
            }

            DrawStatusPanel();
            DrawShopBar();
            DrawHealthBars();
        }

        void DrawConfigError()
        {
            GUILayout.BeginArea(new Rect(10, 10, 480, 200), GUI.skin.box);
            GUILayout.Label("GameHUD: missing references.");
            if (economy == null) GUILayout.Label("  - PlayerEconomy is not assigned");
            if (shop == null)    GUILayout.Label("  - Shop is not assigned");
            if (rounds == null)  GUILayout.Label("  - RoundManager is not assigned");
            if (grid == null)    GUILayout.Label("  - BoardGrid is not assigned");
            GUILayout.Label("Drag the matching GameObjects into each empty field.");
            GUILayout.EndArea();
        }

        void DrawStatusPanel()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, 170), GUI.skin.box);
            GUILayout.Label($"Round: {rounds.round}     Phase: {rounds.Phase}");
            GUILayout.Label($"Gold: {economy.gold}     Level: {economy.level}/{economy.MaxLevel}");
            GUILayout.Label($"Board: {grid.CountPlayerCombatUnits()}/{economy.level}");
            GUILayout.Label($"Interest at round end: +{economy.Interest}");

            if (rounds.LastReward > 0)
            {
                string outcome = rounds.LastWon ? "WIN" : "LOSS";
                GUILayout.Label($"Last result: {outcome}, +{rounds.LastReward}g  " +
                                $"(base {rounds.LastBase} + win {rounds.LastWin} + " +
                                $"kills {rounds.LastKills} + interest {rounds.LastInterest})");
            }

            GUILayout.Label("Right-click a unit to sell it.");
            GUILayout.EndArea();
        }

        void DrawShopBar()
        {
            const float barH = 110f;
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
            else label = $"{data.displayName}\n{data.unitClass} / {data.unitType}\n{data.cost}g";

            bool buyable = prep && data != null && !shop.slotPurchased[i] && economy.CanAfford(data.cost);
            GUI.enabled = buyable;
            if (GUILayout.Button(label, GUILayout.Width(130), GUILayout.Height(85)))
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
                GamePhase.Prep   => "Start Battle",
                GamePhase.Combat => "Battling...",
                GamePhase.Result => "Resolving...",
                _ => "Start Battle",
            };
            if (GUILayout.Button(mainLabel, GUILayout.Height(26)))
                rounds.StartBattle();
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        void DrawHealthBars()
        {
            if (combat == null || !combat.InCombat || worldCamera == null) return;

            DrawHealthBarsFor(combat.PlayerUnits);
            DrawHealthBarsFor(combat.EnemyUnits);
        }

        void DrawHealthBarsFor(System.Collections.Generic.IReadOnlyList<Unit> units)
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
