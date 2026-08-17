using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Knightworld.Presentation
{
    public sealed class OverworldHud : MonoBehaviour
    {
        private Text _title;
        private Text _hint;
        private Text _tooltip;

        public static OverworldHud Create()
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("OverworldHud");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            var hud = canvasGo.AddComponent<OverworldHud>();
            hud.Build();
            return hud;
        }

        public void SetTooltip(string text)
        {
            if (_tooltip == null)
                return;
            _tooltip.text = text ?? "";
        }

        private void Build()
        {
            _title = CreateText("Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -28), 36, TextAnchor.UpperCenter, new Vector2(1200, 70));
            _title.text = "Knightworld";
            _hint = CreateText("Hint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -78), 20, TextAnchor.UpperCenter, new Vector2(1400, 50));
            _hint.text = "Click an unlocked world. Your knight marches the path, then the battle begins.";
            _tooltip = CreateText("Tooltip", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 36), 22, TextAnchor.LowerCenter, new Vector2(1100, 90));
        }

        private Text CreateText(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, int size, TextAnchor align, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, anchorMin.y);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = sizeDelta;
            var text = go.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static Font UiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
