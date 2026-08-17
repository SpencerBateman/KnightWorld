using System;
using Knightworld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Knightworld.Presentation
{
    public sealed class RailroadShopScreen : MonoBehaviour
    {
        public event Action BuySeatClicked;
        public event Action BuyCarriageClicked;
        public event Action BackClicked;

        private RailSession _session;
        private GameObject _root;
        private Text _heading;
        private Text _status;
        private Text _message;
        private Text _seatDetail;
        private Text _carriageDetail;
        private Button _seatButton;
        private Image _seatImage;
        private Text _seatLabel;
        private Button _carriageButton;
        private Image _carriageImage;
        private Text _carriageLabel;

        public bool IsOpen => _root != null && _root.activeSelf;

        public static RailroadShopScreen Create(Transform canvas)
        {
            var host = new GameObject("ShopScreen", typeof(RectTransform));
            var hostRect = host.GetComponent<RectTransform>();
            hostRect.SetParent(canvas, false);
            Stretch(hostRect);
            var screen = host.AddComponent<RailroadShopScreen>();
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
            _heading.text = town.Name + " shop";
            _status.text = $"Score {_session.Score}    Seats {_session.Onboard.Count}/{_session.SeatCount}";
            _message.text = message ?? "Spend your fare on train upgrades.";
            RefreshSeat();
            RefreshCarriage();
        }

        private void RefreshSeat()
        {
            int left = _session.SeatUpgradesLeft;
            bool sold = left <= 0;
            bool canBuy = !sold && _session.Score >= RailSession.SeatUpgradeCost;
            _seatDetail.text = sold
                ? "Sold out. Both extra seats have been fitted."
                : $"One extra passenger seat. {left} of {RailSession.SeatUpgradeStock} left.";
            _seatLabel.text = sold ? "Sold out" : $"+1 seat · {RailSession.SeatUpgradeCost}";
            StyleButton(_seatButton, _seatImage, _seatLabel, canBuy, sold);
        }

        private void RefreshCarriage()
        {
            bool sold = _session.HasCarriage;
            bool canBuy = !sold && _session.Score >= RailSession.CarriageCost;
            _carriageDetail.text = sold
                ? "Owned. The extra carriage is already on the train."
                : "Hitch a passenger carriage. Adds 6 seats, one time only.";
            _carriageLabel.text = sold ? "Owned" : $"+6 seats · {RailSession.CarriageCost}";
            StyleButton(_carriageButton, _carriageImage, _carriageLabel, canBuy, sold);
        }

        private static void StyleButton(Button button, Image image, Text label, bool canBuy, bool sold)
        {
            button.interactable = canBuy;
            if (sold)
                image.color = new Color(0.24f, 0.26f, 0.28f, 0.92f);
            else if (canBuy)
                image.color = new Color(0.22f, 0.52f, 0.34f, 0.95f);
            else
                image.color = new Color(0.32f, 0.34f, 0.36f, 0.92f);
            label.color = canBuy ? Color.white : new Color(0.72f, 0.74f, 0.76f);
        }

        private void Build()
        {
            _root = new GameObject("Panel", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(transform, false);
            Stretch(rootRect);
            var dim = _root.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.05f, 0.78f);

            var card = Box("Card", _root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980, 640), new Color(0.12f, 0.11f, 0.09f, 0.98f));
            _heading = Label(card, "Heading", new Vector2(0.5f, 1f), new Vector2(0, -24), 40, TextAnchor.UpperCenter, new Vector2(860, 52));
            _status = Label(card, "Status", new Vector2(0.5f, 1f), new Vector2(0, -80), 22, TextAnchor.UpperCenter, new Vector2(860, 36));
            _message = Label(card, "Message", new Vector2(0.5f, 1f), new Vector2(0, -118), 20, TextAnchor.UpperCenter, new Vector2(860, 36));

            var seatCard = Box("SeatCard", card.transform, new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(860, 150), new Color(0.08f, 0.09f, 0.10f, 0.95f));
            Label(seatCard, "SeatTitle", new Vector2(0f, 1f), new Vector2(28, -16), 26, TextAnchor.UpperLeft, new Vector2(500, 36)).text = "Extra seat";
            _seatDetail = Label(seatCard, "SeatDetail", new Vector2(0f, 1f), new Vector2(28, -56), 18, TextAnchor.UpperLeft, new Vector2(480, 80));
            var seat = MakeButton(seatCard, "BuySeat", new Vector2(1f, 0.5f), new Vector2(-170, 0), new Vector2(280, 64), () => BuySeatClicked?.Invoke());
            _seatButton = seat.GetComponent<Button>();
            _seatImage = seat.GetComponent<Image>();
            _seatLabel = seat.transform.Find("Label").GetComponent<Text>();

            var carCard = Box("CarriageCard", card.transform, new Vector2(0.5f, 1f), new Vector2(0, -430), new Vector2(860, 150), new Color(0.08f, 0.09f, 0.10f, 0.95f));
            Label(carCard, "CarTitle", new Vector2(0f, 1f), new Vector2(28, -16), 26, TextAnchor.UpperLeft, new Vector2(500, 36)).text = "Passenger carriage";
            _carriageDetail = Label(carCard, "CarDetail", new Vector2(0f, 1f), new Vector2(28, -56), 18, TextAnchor.UpperLeft, new Vector2(480, 80));
            var car = MakeButton(carCard, "BuyCarriage", new Vector2(1f, 0.5f), new Vector2(-170, 0), new Vector2(280, 64), () => BuyCarriageClicked?.Invoke());
            _carriageButton = car.GetComponent<Button>();
            _carriageImage = car.GetComponent<Image>();
            _carriageLabel = car.transform.Find("Label").GetComponent<Text>();

            MakeButton(card, "Back", new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(260, 60), () => BackClicked?.Invoke());
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
    }
}
