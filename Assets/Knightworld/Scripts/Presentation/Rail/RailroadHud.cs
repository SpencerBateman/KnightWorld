using Knightworld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Knightworld.Presentation
{
    public sealed class RailroadHud : MonoBehaviour
    {
        private Text _title;
        private Text _status;
        private Text _cargo;
        private Text _tooltip;

        public static RailroadHud Create()
        {
            if (EventSystem.current == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("RailroadHud");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            var hud = canvasGo.AddComponent<RailroadHud>();
            hud.Build();
            return hud;
        }

        public void SetTooltip(string text)
        {
            if (_tooltip != null)
                _tooltip.text = text ?? "";
        }

        public void Refresh(RailSession session, string banner)
        {
            if (session == null)
                return;
            var town = RailroadGraph.Get(session.CurrentTownId);
            _title.text = "Five Towns";
            _status.text = $"{town.Name}    Score {session.Score}    Seats {session.Onboard.Count}/{RailSession.SeatCount}";
            if (!string.IsNullOrEmpty(banner))
                _status.text += "\n" + banner;

            if (session.Onboard.Count == 0)
            {
                _cargo.text = "Train is empty. Click a waiting passenger to board.";
                return;
            }

            var lines = new System.Text.StringBuilder();
            lines.Append("On board:\n");
            for (int i = 0; i < session.Onboard.Count; i++)
            {
                var person = session.Onboard[i];
                lines.Append(person.Name);
                lines.Append(" → ");
                lines.Append(RailroadGraph.Get(person.DestId).Name);
                if (i < session.Onboard.Count - 1)
                    lines.Append("   ");
            }

            _cargo.text = lines.ToString();
        }

        private void Build()
        {
            _title = CreateText("Title", new Vector2(0.5f, 1f), new Vector2(0, -24), 36, TextAnchor.UpperCenter, new Vector2(900, 60));
            _status = CreateText("Status", new Vector2(0.5f, 1f), new Vector2(0, -72), 22, TextAnchor.UpperCenter, new Vector2(1400, 90));
            _cargo = CreateText("Cargo", new Vector2(0f, 0f), new Vector2(28, 28), 20, TextAnchor.LowerLeft, new Vector2(860, 220));
            _tooltip = CreateText("Tooltip", new Vector2(0.5f, 0f), new Vector2(0, 28), 22, TextAnchor.LowerCenter, new Vector2(1100, 80));
            var hint = CreateText("Hint", new Vector2(1f, 0f), new Vector2(-28, 28), 18, TextAnchor.LowerRight, new Vector2(520, 90));
            hint.text = "Click a town to ride.\nClick a person to board.\nColors match destinations.";
            _title.text = "Five Towns";
        }

        private Text CreateText(string name, Vector2 anchor, Vector2 anchored, int size, TextAnchor align, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchored;
            rect.sizeDelta = sizeDelta;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
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
    }
}
