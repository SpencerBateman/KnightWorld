using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class PlatformStationDesk : MonoBehaviour
    {
        private RailSession _session;
        private RailroadHud _hud;
        private RailroadStationScreen _station;
        private RailroadShopScreen _shop;
        private Bounds _zone;
        private bool _inside;
        private string _banner;

        public bool IsOpen => _station != null && _station.IsOpen;

        public void Initialize(RailSession session, RailroadHud hud, Bounds zone)
        {
            _session = session;
            _hud = hud;
            _station = hud.Station;
            _shop = hud.Shop;
            _zone = zone;
            _station.BoardClicked += OnBoard;
            _station.AlightClicked += OnAlight;
            _station.DepartClicked += OnDepart;
            _station.ShopClicked += OnOpenShop;
            _station.UnlockRouteClicked += OnUnlockRoute;
            _shop.BuySeatClicked += OnBuySeat;
            _shop.BuyCarriageClicked += OnBuyCarriage;
            _shop.BackClicked += OnShopBack;
            _hud.ResetClicked += OnReset;
            _hud.Refresh(_session, null);
        }

        private void OnDestroy()
        {
            if (_station != null)
            {
                _station.BoardClicked -= OnBoard;
                _station.AlightClicked -= OnAlight;
                _station.DepartClicked -= OnDepart;
                _station.ShopClicked -= OnOpenShop;
                _station.UnlockRouteClicked -= OnUnlockRoute;
            }

            if (_shop != null)
            {
                _shop.BuySeatClicked -= OnBuySeat;
                _shop.BuyCarriageClicked -= OnBuyCarriage;
                _shop.BackClicked -= OnShopBack;
            }

            if (_hud != null)
                _hud.ResetClicked -= OnReset;
        }

        public void Tick(Vector3 playerPosition)
        {
            bool nowInside = _zone.Contains(playerPosition);
            if (nowInside && !_inside && !IsOpen)
                OpenStation("Click a traveler to add them. Click a passenger to drop them off.");
            else if (!nowInside && _inside && IsOpen)
            {
                _shop.Close();
                _station.Close();
            }

            _inside = nowInside;
        }

        private void OpenStation(string message)
        {
            _session.EnsureQuestPassenger();
            _station.Open(_session, message);
            _hud.Refresh(_session, _banner);
        }

        private void OnBoard(int passengerId)
        {
            var person = _session.FindWaiting(passengerId);
            if (person == null)
                return;
            if (_session.TryBoard(passengerId))
            {
                _banner = $"{person.Name} boarded for {RailroadGraph.Get(person.DestId).Name}.";
                _station.Refresh(_banner);
                _hud.Refresh(_session, _banner);
                RailSaveStore.Write(_session);
                return;
            }

            _banner = _session.FreeSeats <= 0 ? "No empty seats." : "Could not board.";
            _station.Refresh(_banner);
            _hud.Refresh(_session, _banner);
        }

        private void OnAlight(int passengerId)
        {
            var person = _session.FindOnboard(passengerId);
            if (person == null)
                return;
            string dest = RailroadGraph.Get(person.DestId).Name;
            int fare = person.Fare;
            if (!_session.TryAlight(passengerId, out bool scored))
                return;
            _banner = scored
                ? $"{person.Name} arrived. +{fare} point{(fare == 1 ? "" : "s")}."
                : $"{person.Name} got off early. They wait here for {dest}.";
            _station.Refresh(_banner);
            _hud.Refresh(_session, _banner);
            if (scored)
                _station.PlayScoreBurst(fare);
            RailSaveStore.Write(_session);
        }

        private void OnDepart()
        {
            _shop.Close();
            _station.Close();
            _banner = "Step onto the yellow pad to ride the train.";
            _hud.Refresh(_session, _banner);
        }

        private void OnOpenShop()
        {
            _shop.Open(_session, "Spend your fare on train upgrades.");
            _hud.Refresh(_session, _banner);
        }

        private void OnBuySeat()
        {
            if (_session.TryBuySeatUpgrade())
            {
                AfterUpgrade($"Fitted an extra seat. Capacity is now {_session.SeatCount}.");
                return;
            }

            _banner = _session.SeatUpgradesLeft <= 0
                ? "No extra seats left to buy."
                : $"Need {RailSession.SeatUpgradeCost} points for an extra seat.";
            _shop.Refresh(_banner);
            _hud.Refresh(_session, _banner);
        }

        private void OnBuyCarriage()
        {
            if (_session.TryBuyCarriage())
            {
                AfterUpgrade($"Hitched a passenger carriage. Capacity is now {_session.SeatCount}.");
                return;
            }

            _banner = _session.HasCarriage
                ? "You already have a passenger carriage."
                : $"Need {RailSession.CarriageCost} points for a passenger carriage.";
            _shop.Refresh(_banner);
            _hud.Refresh(_session, _banner);
        }

        private void AfterUpgrade(string banner)
        {
            _banner = banner;
            _hud.Refresh(_session, _banner);
            _shop.Refresh(_banner);
            if (_station.IsOpen)
                _station.Refresh(_banner);
            RailSaveStore.Write(_session);
        }

        private void OnShopBack()
        {
            _shop.Close();
            if (_station.IsOpen)
                _station.Refresh(null);
            _hud.Refresh(_session, _banner);
        }

        private void OnUnlockRoute(string otherTownId)
        {
            var other = RailroadGraph.Get(otherTownId);
            var locked = RailroadGraph.LockedTrack(_session.CurrentTownId, otherTownId);
            if (_session.TryBuyRoute(otherTownId))
            {
                _banner = $"Opened the line to {other.Name}.";
                _station.Refresh(_banner);
                _hud.Refresh(_session, _banner);
                RailSaveStore.Write(_session);
                return;
            }

            if (_session.RouteOwned(_session.CurrentTownId, otherTownId))
                _banner = $"The line to {other.Name} is already open.";
            else if (locked != null)
                _banner = $"Need {locked.Cost} points to unlock the line to {other.Name}.";
            else
                _banner = "That line cannot be bought here.";
            _station.Refresh(_banner);
            _hud.Refresh(_session, _banner);
        }

        private void OnReset()
        {
            RailSaveStore.Clear();
            if (!RailSaveStore.TryLoad(out _session))
            {
                _session = new RailSession(new SeededRandom(11), RailroadGraph.StartTownId);
                _session.SeedWaiting(2);
            }

            _banner = "Save reset.";
            if (_station.IsOpen)
                _station.Open(_session, "Click a traveler to add them. Click a passenger to drop them off.");
            _hud.Refresh(_session, _banner);
        }
    }
}
