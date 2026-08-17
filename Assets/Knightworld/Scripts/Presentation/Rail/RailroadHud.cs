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

        public RailroadStationScreen Station { get; private set; }
        public RailroadShopScreen Shop { get; private set; }

        public static RailroadHud Create()
        {
            if (EventSystem.current == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("RailroadHud", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            var hud = canvasGo.AddComponent<RailroadHud>();
            hud.Build();
            hud.Station = RailroadStationScreen.Create(canvasGo.transform);
            hud.Shop = RailroadShopScreen.Create(canvasGo.transform);
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
            _title.text = RailroadGraph.Map.Title;
            _status.text = $"{town.Name}    Score {session.Score}    Seats {session.Onboard.Count}/{session.SeatCount}";
            if (!string.IsNullOrEmpty(banner))
                _status.text += "\n" + banner;

            _cargo.text = FormatDestinations(session);
        }

        private void Build()
        {
            _title = CreateText("Title", new Vector2(0.5f, 1f), new Vector2(0, -24), 36, TextAnchor.UpperCenter, new Vector2(900, 60));
            _status = CreateText("Status", new Vector2(0.5f, 1f), new Vector2(0, -72), 22, TextAnchor.UpperCenter, new Vector2(1400, 90));
            _cargo = CreateText("Cargo", new Vector2(0f, 0f), new Vector2(28, 28), 20, TextAnchor.LowerLeft, new Vector2(520, 400));
            _tooltip = CreateText("Tooltip", new Vector2(0.5f, 0f), new Vector2(0, 28), 22, TextAnchor.LowerCenter, new Vector2(1100, 80));
            var hint = CreateText("Hint", new Vector2(1f, 0f), new Vector2(-28, 28), 18, TextAnchor.LowerRight, new Vector2(520, 90));
            hint.text = "Click a town to ride.\nOpen the shop for extra seats and a carriage.";
            _title.text = RailroadGraph.Map.Title;
        }

        private Text CreateText(string name, Vector2 anchor, Vector2 anchored, int size, TextAnchor align, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
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
            text.supportRichText = true;
            return text;
        }

        public static string FormatDestinations(RailSession session)
        {
            RailroadMaterials.Ensure();
            var lines = new System.Text.StringBuilder();
            lines.Append("Passenger destinations:");
            var tallies = session.DestinationTallies();
            if (tallies.Count == 0)
            {
                lines.Append("\n<color=#AAAAAA>none</color>");
                return lines.ToString();
            }

            for (int i = 0; i < tallies.Count; i++)
            {
                var tally = tallies[i];
                string hex = ColorUtility.ToHtmlStringRGB(Color.Lerp(RailroadMaterials.TownColor(tally.TownId), Color.white, 0.22f));
                lines.Append("\n<color=#");
                lines.Append(hex);
                lines.Append('>');
                lines.Append(tally.TownName);
                lines.Append(": ");
                lines.Append(tally.Count);
                lines.Append("</color>");
            }

            return lines.ToString();
        }
    }
}
