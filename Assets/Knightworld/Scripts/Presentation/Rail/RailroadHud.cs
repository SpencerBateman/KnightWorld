using System;
using System.Collections;
using Knightworld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Knightworld.Presentation
{
    public sealed class RailroadHud : MonoBehaviour
    {
        private const string ResetLabel = "Reset data";
        private const string ResetConfirmLabel = "Confirm reset";

        public event Action ResetClicked;

        private Text _title;
        private Text _status;
        private Text _cargo;
        private Text _tooltip;
        private GameObject _playHud;
        private GameObject _travelHud;
        private Text _travelTitle;
        private Text _travelTimer;
        private Text _travelSub;
        private RectTransform _resetButton;
        private Text _resetLabel;
        private Coroutine _resetArm;
        private bool _resetArmed;

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
            hud.BuildResetButton();
            return hud;
        }

        public void SetTravelRemaining(float secondsLeft)
        {
            if (_travelTimer == null)
                return;
            _travelTimer.text = FormatCountdown(secondsLeft);
        }

        public void BeginTravel(string fromName, string toName)
        {
            if (_playHud != null)
                _playHud.SetActive(false);
            if (_travelHud != null)
                _travelHud.SetActive(true);
            if (_travelTitle != null)
                _travelTitle.text = "En route to " + toName;
            if (_travelSub != null)
                _travelSub.text = "from " + fromName;
            SetTravelRemaining(0f);
        }

        public void EndTravel()
        {
            if (_travelHud != null)
                _travelHud.SetActive(false);
            if (_playHud != null)
                _playHud.SetActive(true);
        }

        public void SetTooltip(string text)
        {
            if (_tooltip != null)
                _tooltip.text = text ?? "";
        }

        public static string FormatDuration(float seconds)
        {
            int total = Mathf.Max(1, Mathf.RoundToInt(seconds));
            int minutes = total / 60;
            int remainder = total % 60;
            if (minutes <= 0)
                return remainder == 1 ? "1 second" : remainder + " seconds";
            if (remainder == 0)
                return minutes == 1 ? "1 minute" : minutes + " minutes";
            return minutes + " min " + remainder + " sec";
        }

        public static string FormatCountdown(float secondsLeft)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
            int minutes = total / 60;
            int remainder = total % 60;
            return minutes + ":" + remainder.ToString("00");
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
            _playHud = Layer("PlayHud");
            _title = CreateText(_playHud.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0, -24), 36, TextAnchor.UpperCenter, new Vector2(900, 60));
            _status = CreateText(_playHud.transform, "Status", new Vector2(0.5f, 1f), new Vector2(0, -72), 22, TextAnchor.UpperCenter, new Vector2(1400, 90));
            _cargo = CreateText(_playHud.transform, "Cargo", new Vector2(0f, 0f), new Vector2(28, 28), 20, TextAnchor.LowerLeft, new Vector2(520, 400));
            _tooltip = CreateText(_playHud.transform, "Tooltip", new Vector2(0.5f, 0f), new Vector2(0, 28), 22, TextAnchor.LowerCenter, new Vector2(1100, 80));
            var hint = CreateText(_playHud.transform, "Hint", new Vector2(1f, 0f), new Vector2(-28, 28), 18, TextAnchor.LowerRight, new Vector2(520, 90));
            hint.text = "Ride only to a connected station.\nA floating count marks passenger destinations.";
            _title.text = RailroadGraph.Map.Title;

            _travelHud = Layer("TravelHud");
            _travelTitle = CreateText(_travelHud.transform, "TravelTitle", new Vector2(0.5f, 1f), new Vector2(0, -36), 42, TextAnchor.UpperCenter, new Vector2(1200, 64));
            _travelSub = CreateText(_travelHud.transform, "TravelSub", new Vector2(0.5f, 1f), new Vector2(0, -96), 22, TextAnchor.UpperCenter, new Vector2(1000, 40));
            _travelTimer = CreateText(_travelHud.transform, "TravelTimer", new Vector2(0.5f, 0f), new Vector2(0, 72), 84, TextAnchor.LowerCenter, new Vector2(700, 110));
            _travelTimer.alignment = TextAnchor.MiddleCenter;
            var arriving = CreateText(_travelHud.transform, "Arriving", new Vector2(0.5f, 0f), new Vector2(0, 28), 22, TextAnchor.LowerCenter, new Vector2(700, 40));
            arriving.text = "until arrival";
            arriving.alignment = TextAnchor.MiddleCenter;
            _travelHud.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_resetButton != null)
                _resetButton.SetAsLastSibling();
        }

        private void BuildResetButton()
        {
            var go = new GameObject("ResetData", typeof(RectTransform));
            _resetButton = go.GetComponent<RectTransform>();
            _resetButton.SetParent(transform, false);
            _resetButton.anchorMin = new Vector2(1f, 1f);
            _resetButton.anchorMax = new Vector2(1f, 1f);
            _resetButton.pivot = new Vector2(1f, 1f);
            _resetButton.anchoredPosition = new Vector2(-20f, -16f);
            _resetButton.sizeDelta = new Vector2(188f, 40f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.16f, 0.16f, 0.88f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(OnResetClicked);
            var textGo = new GameObject("Label", typeof(RectTransform));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(_resetButton, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            _resetLabel = textGo.AddComponent<Text>();
            _resetLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                               ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _resetLabel.fontSize = 18;
            _resetLabel.alignment = TextAnchor.MiddleCenter;
            _resetLabel.color = new Color(0.92f, 0.84f, 0.84f);
            _resetLabel.text = ResetLabel;
        }

        private void OnResetClicked()
        {
            if (_resetArmed)
            {
                ResetClicked?.Invoke();
                return;
            }

            _resetArmed = true;
            _resetLabel.text = ResetConfirmLabel;
            if (_resetArm != null)
                StopCoroutine(_resetArm);
            _resetArm = StartCoroutine(DisarmReset());
        }

        private IEnumerator DisarmReset()
        {
            yield return new WaitForSecondsRealtime(3f);
            _resetArmed = false;
            if (_resetLabel != null)
                _resetLabel.text = ResetLabel;
            _resetArm = null;
        }

        private GameObject Layer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 anchored, int size, TextAnchor align, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
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
