using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoChess
{
    public class GameHUD : MonoBehaviour
    {
        public PlayerEconomy economy;
        public Shop shop;
        public RoundManager rounds;
        public BoardGrid grid;
        public Inspector inspector;
        public ItemManager items;
        public Camera worldCamera;

        [Header("Pointer blocking")]
        public RectTransform[] pointerBlocks;

        [Header("Panels")]
        public GameObject configPanel;
        public GameObject inspectorPanel;
        public GameObject gameOverPanel;

        [Header("Status")]
        public TMP_Text statusTitle;
        public TMP_Text hpText;
        public TMP_Text goldText;
        public TMP_Text boardText;
        public TMP_Text interestText;
        public TMP_Text lastResultText;

        [Header("Synergies")]
        public TMP_Text[] synergyLines;

        [Header("Shop")]
        public Button[] shopButtons;
        public TMP_Text[] shopLabels;
        public Button refreshButton;
        public TMP_Text refreshButtonLabel;
        public Button levelButton;
        public TMP_Text levelButtonLabel;
        public Button battleButton;
        public TMP_Text battleButtonLabel;

        [Header("Inspector")]
        public TMP_Text inspectorTitle;
        public TMP_Text inspectorBody;
        public Button sellButton;
        public TMP_Text sellButtonLabel;
        public Button equipButton;
        public TMP_Text equipButtonLabel;
        public Button closeInspectorButton;

        [Header("Game over")]
        public TMP_Text gameOverBody;
        public Button restartButton;
        public Button mainMenuButton;

        [Header("Floating combat text")]
        public TMP_Text[] floatingLabels;

        static GameHUD activeHud;

        readonly List<FloatingText> activeFloaters = new();
        bool buttonsBound;

        const float FloaterLifetime = 0.8f;
        const float FloaterRiseSpeed = 1.2f;

        static readonly Color TextColor = new(0.93f, 0.94f, 0.91f, 1f);
        static readonly Color MutedText = new(0.63f, 0.68f, 0.72f, 1f);
        static readonly Color GoldColor = new(1f, 0.78f, 0.25f, 1f);
        static readonly Color GreenColor = new(0.38f, 0.86f, 0.47f, 1f);
        static readonly Color RedColor = new(0.95f, 0.33f, 0.28f, 1f);

        class FloatingText
        {
            public TMP_Text label;
            public Vector3 worldPos;
            public Color color;
            public float age;
        }

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            HideFloatingLabels();
        }

        void Start()
        {
            BindButtons();
            RefreshUi();
        }

        void OnEnable()
        {
            activeHud = this;
            Unit.Damaged += HandleDamaged;
            Unit.Died += HandleDied;
            Unit.Upgraded += HandleUpgraded;
            Unit.AbilityCast += HandleAbilityCast;
        }

        void OnDisable()
        {
            if (activeHud == this) activeHud = null;
            Unit.Damaged -= HandleDamaged;
            Unit.Died -= HandleDied;
            Unit.Upgraded -= HandleUpgraded;
            Unit.AbilityCast -= HandleAbilityCast;
        }

        void Update()
        {
            RefreshUi();
            UpdateFloaters(Time.deltaTime);
        }

        public static bool IsPointerOverHud(Vector2 screenPosition)
        {
            return activeHud != null && activeHud.ContainsPointer(screenPosition);
        }

        bool ContainsPointer(Vector2 screenPosition)
        {
            if (pointerBlocks == null) return false;
            for (int i = 0; i < pointerBlocks.Length; i++)
            {
                var block = pointerBlocks[i];
                if (block != null &&
                    block.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(block, screenPosition, null))
                    return true;
            }
            return false;
        }

        void BindButtons()
        {
            if (buttonsBound) return;
            buttonsBound = true;

            if (shopButtons != null)
            {
                for (int i = 0; i < shopButtons.Length; i++)
                {
                    int slot = i;
                    if (shopButtons[i] != null)
                    {
                        shopButtons[i].onClick.RemoveAllListeners();
                        shopButtons[i].onClick.AddListener(() =>
                        {
                            shop.TryBuy(slot);
                            RefreshUi();
                        });
                    }
                }
            }

            Bind(refreshButton, () => { shop.TryRefresh(); RefreshUi(); });
            Bind(levelButton, () => { economy.TryBuyLevel(); RefreshUi(); });
            Bind(battleButton, () => { rounds.StartBattle(); RefreshUi(); });
            Bind(sellButton, SellInspectedUnit);
            Bind(equipButton, EquipItemOnInspected);
            Bind(closeInspectorButton, () => inspector?.Clear());
            Bind(restartButton, () => rounds.Restart());
            Bind(mainMenuButton, () => rounds.LoadMainMenu());
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        void RefreshUi()
        {
            bool ready = ReferencesReady();
            SetActive(configPanel, !ready);
            if (!ready) return;

            RefreshStatus();
            RefreshSynergies();
            RefreshShop();
            RefreshActions();
            RefreshInspector();
            RefreshGameOver();
        }

        bool ReferencesReady()
        {
            return economy != null && shop != null && rounds != null && grid != null;
        }

        void RefreshStatus()
        {
            SetText(statusTitle, $"Round {rounds.round}  |  {rounds.Phase}");
            SetText(hpText, $"HP: <color=#{ColorHex(economy.hp <= 5 ? RedColor : GreenColor)}>{economy.hp}/{economy.maxHP}</color>");
            SetText(goldText, $"Gold: <color=#{ColorHex(GoldColor)}>{economy.gold}</color>    Level: {economy.level}/{economy.MaxLevel}");
            SetText(boardText, $"Board: {grid.CountPlayerCombatUnits()}/{economy.level}");
            SetText(interestText, $"Interest at round end: +{economy.Interest}g");

            if (rounds.LastReward <= 0 && rounds.LastDamageTaken <= 0)
            {
                SetText(lastResultText, "");
                return;
            }

            string outcome = rounds.LastWon ? "Last: WIN" : "Last: LOSS";
            string damage = rounds.LastDamageTaken > 0 ? $"  -{rounds.LastDamageTaken} HP" : "";

            string note = (items != null && items.HasPending)
                ? $"   <color=#FFC640>Item ready! ({items.PendingCount}) — inspect a unit to Equip</color>"
                : "";

            SetText(lastResultText, $"{outcome}    +{rounds.LastReward}g{damage}{note}");
        }

        void RefreshSynergies()
        {
            if (synergyLines == null) return;

            var s = SynergyEngine.ComputeFromBoard(grid);
            SetSynergyLine(0, "Warrior", s.warriorCount, s.WarriorTier,
                SynergyEngine.DescribeWarrior(1), SynergyEngine.DescribeWarrior(2));
            SetSynergyLine(1, "Mage", s.mageCount, s.MageTier,
                SynergyEngine.DescribeMage(1), SynergyEngine.DescribeMage(2));
            SetSynergyLine(2, "Human", s.humanCount, s.HumanTier,
                SynergyEngine.DescribeHuman(1), SynergyEngine.DescribeHuman(2));
            SetSynergyLine(3, "Beast", s.beastCount, s.BeastTier,
                SynergyEngine.DescribeBeast(1), SynergyEngine.DescribeBeast(2));
            SetSynergyLine(4, "Ranger", s.rangerCount, s.RangerTier,
                SynergyEngine.DescribeRanger(1), SynergyEngine.DescribeRanger(2));
        }

        void SetSynergyLine(int index, string name, int count, int tier, string firstBonus, string secondBonus)
        {
            if (index < 0 || index >= synergyLines.Length || synergyLines[index] == null) return;

            string color = ColorHex(tier > 0 ? GreenColor : MutedText);
            string bonus = tier switch
            {
                2 => secondBonus,
                1 => $"{firstBonus}   -> 3: {secondBonus}",
                _ => $"2: {firstBonus}, 3: {secondBonus}",
            };

            SetText(synergyLines[index], $"<color=#{color}>{name}</color>  {count}/3    {bonus}");
        }

        void RefreshShop()
        {
            if (shopButtons == null || shopLabels == null) return;

            for (int i = 0; i < shopButtons.Length; i++)
            {
                bool visible = i < shop.slotCount;
                SetActive(shopButtons[i] != null ? shopButtons[i].gameObject : null, visible);
                if (!visible) continue;

                var unit = shop.currentSlots != null && i < shop.currentSlots.Length ? shop.currentSlots[i] : null;
                bool buyable = CanBuySlot(i);

                if (shopButtons[i] != null)
                    shopButtons[i].interactable = buyable;

                if (unit == null)
                {
                    SetText(shopLabels[i], "(empty)");
                    continue;
                }

                if (shop.slotPurchased[i])
                {
                    SetText(shopLabels[i], "<color=#9AA0A6>(sold)</color>");
                    continue;
                }

                SetText(shopLabels[i],
                    $"<b>{unit.displayName}</b>  <color=#{ColorHex(GoldColor)}>{unit.cost}g</color>\n" +
                    $"<color=#{ColorHex(MutedText)}>{unit.roleDescription}</color>\n" +
                    $"{unit.unitClass} / {unit.unitType}\n" +
                    $"HP {unit.maxHealth:0}   DMG {unit.attackDamage:0}\n" +
                    $"R {unit.attackRange}   ASPD {unit.attackSpeed:0.##}");
            }
        }

        void RefreshActions()
        {
            SetText(refreshButtonLabel, $"Refresh ({shop.refreshCost}g)");
            SetButton(refreshButton, rounds.Phase == GamePhase.Prep && economy.CanAfford(shop.refreshCost));

            SetText(levelButtonLabel, economy.IsAtMaxLevel ? "Max Level" : $"Level Up ({economy.LevelUpCost}g)");
            SetButton(levelButton, rounds.Phase == GamePhase.Prep && !economy.IsAtMaxLevel && economy.CanAfford(economy.LevelUpCost));

            string battleLabel = rounds.Phase switch
            {
                GamePhase.Prep => "Start Battle",
                GamePhase.Combat => "Battling...",
                GamePhase.Result => "Resolving...",
                GamePhase.GameOver => "Game Over",
                _ => "Start Battle",
            };
            SetText(battleButtonLabel, battleLabel);
            SetButton(battleButton, rounds.Phase == GamePhase.Prep);
        }

        void RefreshInspector()
        {
            bool show = inspector != null && inspector.HasUnit && inspector.Current != null;
            SetActive(inspectorPanel, show);
            if (!show) return;

            var u = inspector.Current;
            SetText(inspectorTitle, $"{u.displayName}  T{u.tier}");
            SetText(inspectorBody,
                $"Team: {u.team}\n" +
                $"Class: {u.unitClass}\n" +
                $"Type: {u.unitType}\n" +
                $"Role: {u.roleDescription}\n\n" +
                $"HP: {u.CurrentHealth:0} / {u.MaxHealth:0}\n" +
                $"Damage: {u.AttackDamage:0.#}\n" +
                $"Range: {u.attackRange}\n" +
                $"Attack Speed: {u.attackSpeed:0.##}/s\n" +
                $"Move Speed: {u.moveSpeed:0.#}");

            if (u.equippedItem != null)
                inspectorBody.text += $"\nItem: {u.equippedItem.itemName} (+{u.equippedItem.amount:0.#} {u.equippedItem.stat})";

            SetText(sellButtonLabel, $"Sell ({u.SellValue}g)");
            SetButton(sellButton, rounds.Phase == GamePhase.Prep && u.team == Team.Player);

            bool canEquip = items != null && items.HasPending && u.team == Team.Player
                            && rounds.Phase == GamePhase.Prep;
            SetActive(equipButton != null ? equipButton.gameObject : null, canEquip);
            if (canEquip)
                SetText(equipButtonLabel, u.CanEquip
                    ? $"Equip {items.PeekNextPending().itemName}"
                    : $"Replace with {items.PeekNextPending().itemName}");
        }

        void RefreshGameOver()
        {
            bool show = rounds.Phase == GamePhase.GameOver;
            SetActive(gameOverPanel, show);
            if (show)
                SetText(gameOverBody, $"Reached round {rounds.round}\nFinal level {economy.level}    Final gold {economy.gold}");
        }

        bool CanBuySlot(int index)
        {
            if (rounds.Phase != GamePhase.Prep) return false;
            if (index < 0 || index >= shop.slotCount) return false;
            if (shop.currentSlots == null || index >= shop.currentSlots.Length) return false;

            var unit = shop.currentSlots[index];
            return unit != null && !shop.slotPurchased[index] && economy.CanAfford(unit.cost);
        }

        void SellInspectedUnit()
        {
            if (inspector == null || inspector.Current == null) return;
            shop.Sell(inspector.Current);
            inspector.Clear();
            RefreshUi();
        }

        void EquipItemOnInspected()
        {
            if (inspector == null || inspector.Current == null || items == null) return;
            var u = inspector.Current;
            if (u.team != Team.Player || !items.HasPending) return;
            var item = items.TakeNextPending();
            if (item == null) return;
            u.RemoveItem();   // clear any existing item so the new one replaces it
            u.Equip(item);
            RefreshUi();
        }

        void HandleDamaged(Unit unit, float damage)
        {
            if (unit == null) return;
            int rounded = Mathf.Max(1, Mathf.RoundToInt(damage));
            Color color = unit.team == Team.Player ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.95f, 0.45f);
            SpawnFloater(unit.transform.position, $"-{rounded}", color);
            if (rounded >= 30) CameraShake.Trigger(0.06f);
        }

        void HandleDied(Unit unit)
        {
            CameraShake.Trigger(0.15f);
        }

        void HandleUpgraded(Unit unit)
        {
            if (unit == null) return;
            SpawnFloater(unit.transform.position, $"T{unit.tier}!", GoldColor);
            CameraShake.Trigger(0.08f);
        }

        void HandleAbilityCast(Unit unit)
        {
            if (unit == null) return;
            SpawnFloater(unit.transform.position, unit.ability.ToString(), new Color(0.6f, 0.8f, 1f));
        }

        void SpawnFloater(Vector3 worldPos, string text, Color color)
        {
            if (floatingLabels == null) return;

            for (int i = 0; i < floatingLabels.Length; i++)
            {
                var label = floatingLabels[i];
                if (label == null || label.gameObject.activeSelf) continue;

                label.text = text;
                label.color = color;
                label.gameObject.SetActive(true);
                activeFloaters.Add(new FloatingText { label = label, worldPos = worldPos, color = color });
                return;
            }
        }

        void UpdateFloaters(float dt)
        {
            if (worldCamera == null) return;

            for (int i = activeFloaters.Count - 1; i >= 0; i--)
            {
                var floater = activeFloaters[i];
                floater.age += dt;

                if (floater.age >= FloaterLifetime)
                {
                    floater.label.gameObject.SetActive(false);
                    activeFloaters.RemoveAt(i);
                    continue;
                }

                float t = floater.age / FloaterLifetime;
                Vector3 world = floater.worldPos + new Vector3(0f, floater.age * FloaterRiseSpeed, 0f);
                Vector3 screen = worldCamera.WorldToScreenPoint(world);

                floater.label.color = new Color(floater.color.r, floater.color.g, floater.color.b, 1f - t);
                floater.label.rectTransform.position = screen;
            }
        }

        void HideFloatingLabels()
        {
            if (floatingLabels == null) return;
            for (int i = 0; i < floatingLabels.Length; i++)
                if (floatingLabels[i] != null)
                    floatingLabels[i].gameObject.SetActive(false);
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text == null) return;
            if (text.text != value) text.text = value;
        }

        static void SetActive(GameObject obj, bool active)
        {
            if (obj != null && obj.activeSelf != active)
                obj.SetActive(active);
        }

        static void SetButton(Button button, bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }

        static string ColorHex(Color color)
        {
            Color32 c = color;
            return $"{c.r:X2}{c.g:X2}{c.b:X2}";
        }
    }
}
