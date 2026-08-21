using System;
using Knightworld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Knightworld.Presentation
{
    public sealed class RailroadStationScreen : MonoBehaviour
    {
        public event Action<int> BoardClicked;
        public event Action<int> AlightClicked;
        public event Action DepartClicked;
        public event Action ShopClicked;
        public event Action<string> UnlockRouteClicked;

        private static readonly Color Gold = new Color(0.95f, 0.78f, 0.16f, 1f);
        private static readonly Color GoldDark = new Color(0.28f, 0.18f, 0.04f, 1f);

        private RailSession _session;
        private GameObject _root;
        private Text _heading;
        private Text _status;
        private Text _message;
        private Text _destinations;
        private Text _scoreBurst;
        private RectTransform _scoreBurstRect;
        private Coroutine _scoreRoutine;
        private Transform _travelers;
        private Transform _passengers;
        private GameObject _routesRow;
        private Transform _routes;

        public bool IsOpen => _root != null && _root.activeSelf;

        public static RailroadStationScreen Create(Transform canvas)
        {
            var host = new GameObject("StationScreen", typeof(RectTransform));
            var hostRect = host.GetComponent<RectTransform>();
            hostRect.SetParent(canvas, false);
            Stretch(hostRect);
            var screen = host.AddComponent<RailroadStationScreen>();
            screen.Build();
            screen.Close();
            return screen;
        }

        public void Open(RailSession session, string message)
        {
            _session = session;
            _root.SetActive(true);
            transform.SetAsLastSibling();
            Refresh(message);
        }

        public void Close()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        public void Refresh(string message)
        {
            if (_session == null || !IsOpen)
                return;
            var town = RailroadGraph.Get(_session.CurrentTownId);
            _heading.text = "Arrived at " + town.Name;
            _status.text = $"Score {_session.Score}    Seats {_session.Onboard.Count}/{_session.SeatCount}";
            _message.text = message ?? "Click a traveler to board. Click a passenger to drop them off.";
            _destinations.text = RailroadHud.FormatDestinations(_session);
            FillColumn(_travelers, _session.WaitingHere, false);
            FillColumn(_passengers, _session.OnboardReadyFirst(), true);
            FillRoutes();
        }

        public void PlayScoreBurst(int points)
        {
            if (_scoreBurst == null)
                return;
            if (_scoreRoutine != null)
                StopCoroutine(_scoreRoutine);
            _scoreRoutine = StartCoroutine(ScoreBurst(points));
        }

        private void Build()
        {
            _root = new GameObject("Panel", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(transform, false);
            Stretch(rootRect);
            var dim = _root.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.05f, 0.72f);

            var card = Box("Card", _root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280, 820), new Color(0.10f, 0.12f, 0.16f, 0.98f));
            _heading = Label(card, "Heading", new Vector2(0.5f, 1f), new Vector2(0, -20), 40, TextAnchor.UpperCenter, new Vector2(1100, 56));
            _status = Label(card, "Status", new Vector2(0.5f, 1f), new Vector2(0, -76), 22, TextAnchor.UpperCenter, new Vector2(1100, 36));
            _message = Label(card, "Message", new Vector2(0.5f, 1f), new Vector2(0, -112), 20, TextAnchor.UpperCenter, new Vector2(1100, 36));
            _destinations = Label(card, "Destinations", new Vector2(0.5f, 1f), new Vector2(0, -148), 18, TextAnchor.UpperCenter, new Vector2(1180, 170));
            _destinations.supportRichText = true;

            Label(card, "TravelersHeader", new Vector2(0f, 1f), new Vector2(56, -324), 24, TextAnchor.UpperLeft, new Vector2(540, 36)).text = "Travelers at this station";
            Label(card, "PassengersHeader", new Vector2(1f, 1f), new Vector2(-56, -324), 24, TextAnchor.UpperRight, new Vector2(540, 36)).text = "Passengers on the train";

            _travelers = Column(card, "Travelers", new Vector2(0f, 1f), new Vector2(48, -364), new Vector2(560, 280));
            _passengers = Column(card, "Passengers", new Vector2(1f, 1f), new Vector2(-48, -364), new Vector2(560, 280));

            _routesRow = Box("Routes", card.transform, new Vector2(0.5f, 0f), new Vector2(0, 118), new Vector2(1180, 72), new Color(0.08f, 0.09f, 0.12f, 0.95f));
            _routes = _routesRow.transform;
            _routesRow.SetActive(false);

            var shop = MakeButton(card, "Shop", new Vector2(0.5f, 0f), new Vector2(-170, 36), new Vector2(280, 64), () => ShopClicked?.Invoke());
            shop.GetComponent<Image>().color = new Color(0.22f, 0.52f, 0.34f, 0.95f);
            MakeButton(card, "Depart", new Vector2(0.5f, 0f), new Vector2(170, 36), new Vector2(280, 64), () => DepartClicked?.Invoke());

            _scoreBurst = Label(_root, "ScoreBurst", new Vector2(0.5f, 0.5f), new Vector2(0, 90), 92, TextAnchor.MiddleCenter, new Vector2(600, 140));
            _scoreBurst.alignment = TextAnchor.MiddleCenter;
            _scoreBurst.color = Gold;
            _scoreBurst.raycastTarget = false;
            _scoreBurst.text = "+1";
            _scoreBurstRect = _scoreBurst.GetComponent<RectTransform>();
            _scoreBurstRect.pivot = new Vector2(0.5f, 0.5f);
            _scoreBurst.gameObject.SetActive(false);
        }

        private void FillColumn(Transform column, System.Collections.Generic.IReadOnlyList<Passenger> people, bool onboard)
        {
            for (int i = column.childCount - 1; i >= 0; i--)
                Destroy(column.GetChild(i).gameObject);

            if (people.Count == 0)
            {
                var contentRect = column.GetComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(0f, 64f);
                var empty = Label(column.gameObject, "Empty", new Vector2(0.5f, 1f), new Vector2(0, -8), 20, TextAnchor.UpperCenter, new Vector2(520, 48));
                empty.text = onboard ? "No passengers on board." : "No travelers waiting.";
                empty.color = new Color(0.75f, 0.78f, 0.82f);
                return;
            }

            column.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 12f + people.Count * 70f);

            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                bool here = onboard && person.DestId == _session.CurrentTownId;
                string destName = RailroadGraph.Get(person.DestId).Name;
                string label = onboard
                    ? (here ? $"{person.Name}  ·  drop off here    {person.Fare}" : $"{person.Name}  →  {destName}    {person.Fare}")
                    : $"{person.Name}  →  {destName}    {person.Fare}";
                if (person.IsQuest)
                    label = "Quest · " + label;
                Color color = person.IsQuest
                    ? Gold
                    : here
                    ? Gold
                    : Mix(RailroadMaterials.TownColor(person.DestId), new Color(0.12f, 0.14f, 0.18f), 0.45f);
                int id = person.Id;
                var button = MakeButton(column.gameObject, person.Name, new Vector2(0.5f, 1f), new Vector2(0, -8 - i * 70), new Vector2(540, 62), () =>
                {
                    if (onboard)
                        AlightClicked?.Invoke(id);
                    else
                        BoardClicked?.Invoke(id);
                });
                button.GetComponent<Image>().color = color;
                var labelText = button.transform.Find("Label").GetComponent<Text>();
                labelText.text = label;
                if (here)
                    labelText.color = GoldDark;
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }
        }

        private void FillRoutes()
        {
            for (int i = _routes.childCount - 1; i >= 0; i--)
                Destroy(_routes.GetChild(i).gameObject);

            var locked = RailroadGraph.LockedFrom(_session.CurrentTownId);
            var offers = new System.Collections.Generic.List<LockedTrackDef>();
            for (int i = 0; i < locked.Count; i++)
            {
                string other = locked[i].Other(_session.CurrentTownId);
                if (!_session.RouteOwned(_session.CurrentTownId, other))
                    offers.Add(locked[i]);
            }

            if (offers.Count == 0)
            {
                _routesRow.SetActive(false);
                return;
            }

            _routesRow.SetActive(true);
            float width = 360f;
            float gap = 16f;
            float total = offers.Count * width + (offers.Count - 1) * gap;
            float start = -total * 0.5f + width * 0.5f;
            for (int i = 0; i < offers.Count; i++)
            {
                var route = offers[i];
                string otherId = route.Other(_session.CurrentTownId);
                string otherName = RailroadGraph.Get(otherId).Name;
                bool canBuy = _session.Score >= route.Cost;
                var button = MakeButton(_routesRow, otherName, new Vector2(0.5f, 0.5f), new Vector2(start + i * (width + gap), 0), new Vector2(width, 52), () =>
                {
                    UnlockRouteClicked?.Invoke(otherId);
                });
                button.GetComponent<Image>().color = canBuy
                    ? new Color(0.22f, 0.52f, 0.34f, 0.95f)
                    : new Color(0.32f, 0.34f, 0.36f, 0.92f);
                button.transform.Find("Label").GetComponent<Text>().text = $"Unlock {otherName}  ·  {route.Cost}";
            }
        }

        private System.Collections.IEnumerator ScoreBurst(int points)
        {
            _scoreBurst.gameObject.SetActive(true);
            _scoreBurst.text = "+" + points;
            Color start = Gold;
            Vector2 from = new Vector2(0f, 40f);
            Vector2 to = new Vector2(0f, 220f);
            float duration = 0.9f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float punch = t < 0.18f ? Mathf.Lerp(0.4f, 1.45f, t / 0.18f) : Mathf.Lerp(1.45f, 1f, (t - 0.18f) / 0.82f);
                _scoreBurstRect.anchoredPosition = Vector2.Lerp(from, to, t * t);
                _scoreBurstRect.localScale = Vector3.one * punch;
                Color color = start;
                color.a = t < 0.45f ? 1f : 1f - (t - 0.45f) / 0.55f;
                _scoreBurst.color = color;
                yield return null;
            }

            _scoreBurst.gameObject.SetActive(false);
            _scoreBurstRect.localScale = Vector3.one;
            _scoreRoutine = null;
        }

        private static GameObject Box(string name, Transform parent, Vector2 anchor, Vector2 anchored, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Transform Column(GameObject parent, string name, Vector2 anchor, Vector2 anchored, Vector2 size)
        {
            var viewport = new GameObject(name, typeof(RectTransform));
            var rect = viewport.GetComponent<RectTransform>();
            rect.SetParent(parent.transform, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, 1f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
            var image = viewport.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.12f);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.SetParent(rect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, size.y);

            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = rect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content.transform;
        }

        private static GameObject MakeButton(GameObject parent, string name, Vector2 anchor, Vector2 anchored, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent.transform, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.78f, 0.22f, 0.18f, 0.95f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(click);
            var textGo = new GameObject("Label", typeof(RectTransform));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            Stretch(textRect);
            var text = textGo.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = name;
            return go;
        }

        private static Text Label(GameObject parent, string name, Vector2 anchor, Vector2 anchored, int size, TextAnchor align, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent.transform, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchored;
            rect.sizeDelta = sizeDelta;
            var text = go.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font UiFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Color Mix(Color a, Color b, float t)
        {
            return Color.Lerp(b, a, t);
        }
    }
}
