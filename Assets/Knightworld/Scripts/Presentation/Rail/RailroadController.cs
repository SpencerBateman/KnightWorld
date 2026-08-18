using System.Collections;
using Knightworld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Knightworld.Presentation
{
    public sealed class RailroadController : MonoBehaviour
    {
        public const string SceneName = "Railroad";

        private RailSession _session;
        private RailroadView _view;
        private RailroadHud _hud;
        private RailroadStationScreen _station;
        private RailroadShopScreen _shop;
        private Camera _camera;
        private bool _busy;
        private string _banner;

        public void Initialize(RailSession session, RailroadView view, RailroadHud hud, Camera worldCamera)
        {
            _session = session;
            _view = view;
            _hud = hud;
            _station = hud.Station;
            _shop = hud.Shop;
            _camera = worldCamera;
            _station.BoardClicked += OnBoard;
            _station.AlightClicked += OnAlight;
            _station.DepartClicked += OnDepart;
            _station.ShopClicked += OnOpenShop;
            _station.UnlockRouteClicked += OnUnlockRoute;
            _shop.BuySeatClicked += OnBuySeat;
            _shop.BuyCarriageClicked += OnBuyCarriage;
            _shop.BackClicked += OnShopBack;
            _view.RefreshPassengers(_session);
            _hud.Refresh(_session, null);
            OpenStation("Click a traveler to add them. Click a passenger to drop them off.");
        }

        private bool UiOpen => (_station != null && _station.IsOpen) || (_shop != null && _shop.IsOpen);

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
        }

        private void Update()
        {
            if (_view == null || _session == null)
                return;
            _view.FaceLabels(_camera);
            if (_busy || UiOpen)
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _hud.SetTooltip("");
                return;
            }

            TryHover();
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                HandleClick();
        }

        private void OpenStation(string message)
        {
            _station.Open(_session, message);
            _view.RefreshPassengers(_session);
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
                _view.RefreshPassengers(_session);
                _hud.Refresh(_session, _banner);
                _station.Refresh(_banner);
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
            _view.RefreshPassengers(_session);
            _hud.Refresh(_session, _banner);
            _station.Refresh(_banner);
            if (scored)
                _station.PlayScoreBurst(fare);
        }

        private void OnOpenShop()
        {
            OpenShop("Spend your fare on train upgrades.");
        }

        private void OpenShop(string message)
        {
            _shop.Open(_session, message);
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
            _view.SyncSeats(_session.SeatCount);
            _view.RefreshPassengers(_session);
            _hud.Refresh(_session, _banner);
            _shop.Refresh(_banner);
            if (_station.IsOpen)
                _station.Refresh(_banner);
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
                _view.UnlockTrack(_session.CurrentTownId, otherTownId);
                _banner = $"Opened the line to {other.Name}.";
                _view.RefreshPassengers(_session);
                _hud.Refresh(_session, _banner);
                _station.Refresh(_banner);
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

        private void OnDepart()
        {
            _shop.Close();
            _station.Close();
            _banner = "Choose a connected station to ride to.";
            _hud.Refresh(_session, _banner);
        }

        private void TryHover()
        {
            if (TryPickShop(out var shopTown) && shopTown == _session.CurrentTownId)
            {
                _hud.SetTooltip("Open the shop.");
                return;
            }

            if (!TryPickTown(out var townId))
            {
                _hud.SetTooltip("");
                return;
            }

            var town = RailroadGraph.Get(townId);
            if (town.Id == _session.CurrentTownId)
                _hud.SetTooltip($"Click to open the {town.Name} platform.");
            else if (!_session.CanRide(_session.CurrentTownId, town.Id))
                _hud.SetTooltip(LockedRideLabel(town));
            else
                _hud.SetTooltip($"Ride to {town.Name}. {TravelLabel(town.Id)}");
        }

        private void HandleClick()
        {
            if (TryPickShop(out var shopTown) && shopTown == _session.CurrentTownId)
            {
                OpenShop("Spend your fare on train upgrades.");
                return;
            }

            if (!TryPickTown(out var townId))
                return;
            if (townId == _session.CurrentTownId)
            {
                OpenStation("Click a traveler to add them. Click a passenger to drop them off.");
                return;
            }

            if (!RailroadGraph.AreLinked(_session.CurrentTownId, townId))
            {
                var blocked = RailroadGraph.Get(townId);
                _banner = $"You can only ride to a connected station. {blocked.Name} is not next to here.";
                _hud.Refresh(_session, _banner);
                return;
            }

            if (!_session.CanRide(_session.CurrentTownId, townId))
            {
                _banner = LockedRideLabel(RailroadGraph.Get(townId));
                _hud.Refresh(_session, _banner);
                return;
            }

            StartCoroutine(Ride(_session.CurrentTownId, townId));
        }

        private IEnumerator Ride(string fromId, string toId)
        {
            _busy = true;
            _hud.SetTooltip("");
            _session.RollPassengersOnMove();
            _view.RefreshPassengers(_session);
            var fromTown = RailroadGraph.Get(fromId);
            var toTown = RailroadGraph.Get(toId);
            Vector3 from = _view.TrainPos(fromId);
            Vector3 to = _view.TrainPos(toId);
            Vector3 delta = to - from;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
                _view.Train.rotation = Quaternion.LookRotation(delta);

            float hopSeconds = RailroadGraph.TravelSeconds(RailroadGraph.Distance(fromId, toId));
            var iso = _camera != null ? _camera.GetComponent<IsoCameraController>() : null;
            if (iso != null)
                iso.FrameRoute(from, to);
            _hud.BeginTravel(fromTown.Name, toTown.Name);
            _hud.SetTravelRemaining(hopSeconds);

            float elapsed = 0f;
            while (elapsed < hopSeconds)
            {
                elapsed += Time.deltaTime;
                float remaining = hopSeconds - elapsed;
                _hud.SetTravelRemaining(remaining);
                float t = Mathf.Clamp01(elapsed / hopSeconds);
                t = t * t * (3f - 2f * t);
                _view.Train.position = Vector3.Lerp(from, to, t);
                if (iso != null)
                    iso.Follow(_view.Train.position);
                yield return null;
            }

            _view.Train.position = to;
            _hud.EndTravel();
            if (iso != null)
                iso.UnlockOn(to);

            _session.Arrive(toId);
            _view.SnapTrain(_session.CurrentTownId);
            _view.RefreshPassengers(_session);
            _banner = "Arrived at " + toTown.Name + ".";
            _hud.Refresh(_session, _banner);
            _busy = false;
            OpenStation($"Arrived at {toTown.Name}. Board travelers, drop passengers, or visit the shop.");
        }

        private bool TryPickTown(out string townId)
        {
            townId = null;
            var camera = _camera != null ? _camera : Camera.main;
            if (camera == null || Mouse.current == null)
                return false;
            var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 140f);
            float townDist = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var town = hits[i].collider.GetComponentInParent<TownMarker>();
                if (town == null || hits[i].distance >= townDist)
                    continue;
                townId = town.TownId;
                townDist = hits[i].distance;
            }

            return townId != null;
        }

        private bool TryPickShop(out string townId)
        {
            townId = null;
            var camera = _camera != null ? _camera : Camera.main;
            if (camera == null || Mouse.current == null)
                return false;
            var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 140f);
            float shopDist = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var shop = hits[i].collider.GetComponentInParent<ShopMarker>();
                if (shop == null || hits[i].distance >= shopDist)
                    continue;
                townId = shop.TownId;
                shopDist = hits[i].distance;
            }

            return townId != null;
        }

        private string LockedRideLabel(TownDef town)
        {
            var locked = RailroadGraph.LockedTrack(_session.CurrentTownId, town.Id);
            if (locked != null)
                return $"The line to {town.Name} is locked. Buy it at this station for {locked.Cost}.";
            return $"{town.Name} is not on a connecting track.";
        }

        private string TravelLabel(string townId)
        {
            float seconds = RailroadGraph.TravelSeconds(RailroadGraph.Distance(_session.CurrentTownId, townId));
            return RailroadHud.FormatDuration(seconds);
        }
    }
}
