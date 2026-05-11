using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AutoChess.EditorTools
{
    public static class HudCanvasBuilder
    {
        static readonly Color Panel = new(0.055f, 0.065f, 0.085f, 0.92f);
        static readonly Color PanelStrong = new(0.08f, 0.095f, 0.12f, 0.96f);
        static readonly Color Border = new(1f, 0.78f, 0.25f, 0.45f);
        static readonly Color Text = new(0.93f, 0.94f, 0.91f, 1f);
        static readonly Color Muted = new(0.63f, 0.68f, 0.72f, 1f);
        static readonly Color Gold = new(1f, 0.78f, 0.25f, 1f);
        static readonly Color Blue = new(0.42f, 0.74f, 1f, 1f);
        static readonly Color Red = new(0.95f, 0.33f, 0.28f, 1f);
        static readonly Color Green = new(0.38f, 0.86f, 0.47f, 1f);

        [MenuItem("AutoChess/Rebuild HUD Canvas")]
        public static void Build()
        {
            var hud = Object.FindFirstObjectByType<GameHUD>();
            if (hud == null)
            {
                Debug.LogError("GameHUD not found in the open scene.");
                return;
            }

            DeleteIfExists("HUD Canvas");
            EnsureEventSystem();

            var canvasRoot = CreateCanvas(hud.transform);
            var pointerBlocks = new List<RectTransform>();

            BuildStatus(canvasRoot, hud, pointerBlocks);
            BuildSynergies(canvasRoot, hud, pointerBlocks);
            BuildShop(canvasRoot, hud, pointerBlocks);
            BuildInspector(canvasRoot, hud, pointerBlocks);
            BuildGameOver(canvasRoot, hud, pointerBlocks);
            BuildConfig(canvasRoot, hud, pointerBlocks);
            BuildFloaters(canvasRoot, hud);

            hud.pointerBlocks = pointerBlocks.ToArray();

            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("HUD Canvas rebuilt.");
        }

        static void DeleteIfExists(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        static void EnsureEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                var module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                var method = typeof(InputSystemUIInputModule).GetMethod("AssignDefaultActions",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(module, null);
            }
        }

        static RectTransform CreateCanvas(Transform parent)
        {
            var go = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            return go.GetComponent<RectTransform>();
        }

        static void BuildStatus(RectTransform root, GameHUD hud, List<RectTransform> blocks)
        {
            var panel = CreatePanel(root, "Status Panel", Anchor.TopLeft, new Vector2(16f, -16f), new Vector2(360f, 190f), Panel);
            blocks.Add(panel);
            Vertical(panel, 5, new RectOffset(14, 14, 12, 12));

            hud.statusTitle = TextBlock(panel, "Status Title", 22, Gold, FontStyles.Bold);
            hud.statusTitle.text = "Round 1  |  Prep";
            hud.hpText = TextBlock(panel, "HP Text", 15, Text, FontStyles.Bold);
            hud.hpText.text = "HP: 20/20";
            hud.goldText = TextBlock(panel, "Gold Text", 15, Text, FontStyles.Normal);
            hud.goldText.text = "Gold: 10    Level: 2/7";
            hud.boardText = TextBlock(panel, "Board Text", 15, Text, FontStyles.Normal);
            hud.boardText.text = "Board: 0/2";
            hud.interestText = TextBlock(panel, "Interest Text", 14, Muted, FontStyles.Normal);
            hud.interestText.text = "Interest at round end: +1g";
            hud.lastResultText = TextBlock(panel, "Last Result Text", 13, Muted, FontStyles.Normal);
            hud.lastResultText.text = "";
        }

        static void BuildSynergies(RectTransform root, GameHUD hud, List<RectTransform> blocks)
        {
            var panel = CreatePanel(root, "Synergy Panel", Anchor.TopRight, new Vector2(-16f, -16f), new Vector2(380f, 150f), Panel);
            blocks.Add(panel);
            Vertical(panel, 7, new RectOffset(14, 14, 12, 12));

            var title = TextBlock(panel, "Synergy Title", 18, Blue, FontStyles.Bold);
            title.text = "Synergies";

            hud.synergyLines = new TMP_Text[4];
            for (int i = 0; i < hud.synergyLines.Length; i++)
                hud.synergyLines[i] = TextBlock(panel, $"Synergy Line {i + 1}", 14, Text, FontStyles.Normal);
        }

        static void BuildShop(RectTransform root, GameHUD hud, List<RectTransform> blocks)
        {
            var panel = CreatePanel(root, "Shop Bar", Anchor.BottomStretch, new Vector2(0f, 16f), new Vector2(-32f, 140f), PanelStrong);
            blocks.Add(panel);

            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            hud.shopButtons = new Button[5];
            hud.shopLabels = new TMP_Text[5];
            for (int i = 0; i < 5; i++)
            {
                var button = Button(panel, $"Shop Slot {i + 1}", "", new Vector2(150f, 112f), new Color(0.18f, 0.22f, 0.26f, 1f), 13);
                button.GetComponent<LayoutElement>().flexibleWidth = 1f;
                var label = button.GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.TopLeft;
                label.margin = new Vector4(8f, 7f, 8f, 5f);
                hud.shopButtons[i] = button;
                hud.shopLabels[i] = label;
            }

            var actions = new GameObject("Action Column", typeof(RectTransform)).GetComponent<RectTransform>();
            actions.SetParent(panel, false);
            actions.gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;
            Vertical(actions, 8, new RectOffset(0, 0, 0, 0));

            hud.refreshButton = Button(actions, "Refresh Button", "Refresh", new Vector2(170f, 34f), Gold, 15);
            hud.refreshButtonLabel = hud.refreshButton.GetComponentInChildren<TMP_Text>();
            hud.levelButton = Button(actions, "Level Button", "Level Up", new Vector2(170f, 34f), Blue, 15);
            hud.levelButtonLabel = hud.levelButton.GetComponentInChildren<TMP_Text>();
            hud.battleButton = Button(actions, "Battle Button", "Start Battle", new Vector2(170f, 42f), Green, 16);
            hud.battleButtonLabel = hud.battleButton.GetComponentInChildren<TMP_Text>();
        }

        static void BuildInspector(RectTransform root, GameHUD hud, List<RectTransform> blocks)
        {
            var panel = CreatePanel(root, "Inspector Panel", Anchor.TopRight, new Vector2(-16f, -182f), new Vector2(310f, 370f), Panel);
            blocks.Add(panel);
            Vertical(panel, 8, new RectOffset(14, 14, 14, 14));

            hud.inspectorPanel = panel.gameObject;
            hud.inspectorTitle = TextBlock(panel, "Inspector Title", 20, Gold, FontStyles.Bold);
            hud.inspectorBody = TextBlock(panel, "Inspector Body", 14, Text, FontStyles.Normal);
            hud.inspectorBody.enableWordWrapping = true;
            hud.inspectorBody.overflowMode = TextOverflowModes.Overflow;
            SetTextHeight(hud.inspectorBody, 210f);

            hud.sellButton = Button(panel, "Sell Button", "Sell", new Vector2(280f, 34f), Red, 15);
            hud.sellButtonLabel = hud.sellButton.GetComponentInChildren<TMP_Text>();
            hud.closeInspectorButton = Button(panel, "Close Button", "Close", new Vector2(280f, 30f), new Color(0.22f, 0.25f, 0.29f, 1f), 14);
            panel.gameObject.SetActive(false);
        }

        static void BuildGameOver(RectTransform root, GameHUD hud, List<RectTransform> blocks)
        {
            var overlay = CreatePanel(root, "Game Over Overlay", Anchor.Stretch, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.58f));
            blocks.Add(overlay);
            hud.gameOverPanel = overlay.gameObject;

            var panel = CreatePanel(overlay, "Game Over Panel", Anchor.Center, Vector2.zero, new Vector2(420f, 230f), PanelStrong);
            Vertical(panel, 12, new RectOffset(20, 20, 18, 18));

            var title = TextBlock(panel, "Game Over Title", 30, Red, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;
            title.text = "GAME OVER";

            hud.gameOverBody = TextBlock(panel, "Game Over Body", 16, Text, FontStyles.Normal);
            hud.gameOverBody.alignment = TextAlignmentOptions.Center;
            hud.restartButton = Button(panel, "Restart Button", "Restart", new Vector2(180f, 42f), Gold, 17);
            overlay.gameObject.SetActive(false);
        }

        static void BuildConfig(RectTransform root, GameHUD hud, List<RectTransform> blocks)
        {
            var panel = CreatePanel(root, "Config Error Panel", Anchor.Center, Vector2.zero, new Vector2(520f, 210f), PanelStrong);
            blocks.Add(panel);
            hud.configPanel = panel.gameObject;
            Vertical(panel, 8, new RectOffset(18, 18, 16, 16));

            var title = TextBlock(panel, "Config Title", 22, Red, FontStyles.Bold);
            title.text = "GameHUD missing references";
            var body = TextBlock(panel, "Config Body", 15, Text, FontStyles.Normal);
            body.enableWordWrapping = true;
            body.text = "Assign PlayerEconomy, Shop, RoundManager and BoardGrid in the GameManager object.";
            panel.gameObject.SetActive(false);
        }

        static void BuildFloaters(RectTransform root, GameHUD hud)
        {
            var container = new GameObject("Floating Combat Text", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(root, false);
            Stretch(container);

            hud.floatingLabels = new TMP_Text[24];
            for (int i = 0; i < hud.floatingLabels.Length; i++)
            {
                var label = TextBlock(container, $"Floating Text {i + 1}", 18, Gold, FontStyles.Bold);
                label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(120f, 32f);
                label.gameObject.SetActive(false);
                hud.floatingLabels[i] = label;
            }
        }

        static RectTransform CreatePanel(RectTransform parent, string name, Anchor anchor, Vector2 pos, Vector2 size, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var image = rect.GetComponent<Image>();
            image.color = color;

            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(1f, -1f);

            return rect;
        }

        static TMP_Text TextBlock(RectTransform parent, string name, int size, Color color, FontStyles style)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TMP_Text>();
            text.transform.SetParent(parent, false);
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;

            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = size + 6f;
            layout.preferredHeight = size + 6f;
            layout.flexibleHeight = 0f;

            return text;
        }

        static void SetTextHeight(TMP_Text text, float height)
        {
            if (text == null || !text.TryGetComponent(out LayoutElement layout))
                return;

            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 1f;
        }

        static Button Button(RectTransform parent, string name, string text, Vector2 size, Color accent, int fontSize)
        {
            var rect = CreatePanel(parent, name, Anchor.Center, Vector2.zero, size,
                new Color(accent.r * 0.48f, accent.g * 0.48f, accent.b * 0.48f, 0.96f));
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = size.y;

            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;

            var label = TextBlock(rect, "Label", fontSize, Text, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            label.text = text;
            Stretch(label.rectTransform, new RectOffset(7, 7, 4, 4));

            return button;
        }

        static void Vertical(RectTransform rect, int spacing, RectOffset padding)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rect, RectOffset offset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(offset.left, offset.bottom);
            rect.offsetMax = new Vector2(-offset.right, -offset.top);
        }

        static void ApplyAnchor(RectTransform rect, Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.TopLeft:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                    break;
                case Anchor.TopRight:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
                    break;
                case Anchor.BottomStretch:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    break;
                case Anchor.Stretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                default:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }

        enum Anchor
        {
            Center,
            TopLeft,
            TopRight,
            BottomStretch,
            Stretch,
        }
    }
}
