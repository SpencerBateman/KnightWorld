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
        private const float SpawnSeconds = 7f;

        private RailSession _session;
        private RailroadView _view;
        private RailroadHud _hud;
        private RailroadStationScreen _station;
        private Camera _camera;
        private bool _busy;
        private float _spawnTimer;
        private string _banner;

        public void Initialize(RailSession session, RailroadView view, RailroadHud hud, Camera worldCamera)
        {
            _session = session;
            _view = view;
            _hud = hud;
            _station = hud.Station;
            _camera = worldCamera;
            _spawnTimer = SpawnSeconds;
            _station.BoardClicked += OnBoard;
            _station.AlightClicked += OnAlight;
            _station.DepartClicked += OnDepart;
            _station.BuySeatsClicked += OnBuySeats;
            _view.RefreshPassengers(_session);
            _hud.Refresh(_session, null);
            OpenStation("Click a traveler to add them. Click a passenger to drop them off. Buy extra seats at the store.");
        }

        private void OnDestroy()
        {
            if (_station == null)
                return;
            _station.BoardClicked -= OnBoard;
            _station.AlightClicked -= OnAlight;
            _station.DepartClicked -= OnDepart;
            _station.BuySeatsClicked -= OnBuySeats;
        }

        private void Update()
        {
            if (_view == null || _session == null)
                return;
            _view.FaceLabels(_camera);
            if (!_busy)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    if (_session.TrySpawnPassenger())
                    {
                        _view.RefreshPassengers(_session);
                        if (_station.IsOpen)
                            _station.Refresh(null);
                    }

                    _spawnTimer = SpawnSeconds;
                    _hud.Refresh(_session, _banner);
                }
            }

            if (_busy || _station.IsOpen)
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

        private void OnBuySeats()
        {
            if (_session.TryBuySeatUpgrade())
            {
                _view.SyncSeats(_session.SeatCount);
                _banner = $"Bought two seats. Capacity is now {_session.SeatCount}.";
                _view.RefreshPassengers(_session);
                _hud.Refresh(_session, _banner);
                _station.Refresh(_banner);
                return;
            }

            _banner = $"Need {RailSession.SeatUpgradeCost} points for two seats.";
            _station.Refresh(_banner);
            _hud.Refresh(_session, _banner);
        }

        private void OnDepart()
        {
            _station.Close();
            _banner = "Choose a town to ride to.";
            _hud.Refresh(_session, _banner);
        }

        private void TryHover()
        {
            if (!TryPickTown(out var townId))
            {
                _hud.SetTooltip("");
                return;
            }

            var town = RailroadGraph.Get(townId);
            if (town.Id == _session.CurrentTownId)
                _hud.SetTooltip($"Click to open the {town.Name} platform.");
            else
                _hud.SetTooltip($"Ride the rails to {town.Name}. {TravelLabel(town.Id)}");
        }

        private void HandleClick()
        {
            if (!TryPickTown(out var townId))
                return;
            if (townId == _session.CurrentTownId)
            {
                OpenStation("Click a traveler to add them. Click a passenger to drop them off. Buy extra seats at the store.");
                return;
            }

            var route = RailroadGraph.FindRoute(_session.CurrentTownId, townId);
            if (route == null || route.Count < 2)
                return;
            StartCoroutine(Ride(route));
        }

        private IEnumerator Ride(System.Collections.Generic.List<string> route)
        {
            _busy = true;
            _hud.SetTooltip("");
            for (int i = 1; i < route.Count; i++)
                yield return Hop(route[i - 1], route[i]);

            _session.Arrive(route[route.Count - 1]);
            _view.SnapTrain(_session.CurrentTownId);
            _view.RefreshPassengers(_session);
            var town = RailroadGraph.Get(_session.CurrentTownId);
            _banner = "Arrived at " + town.Name + ".";
            _hud.Refresh(_session, _banner);
            _busy = false;
            OpenStation($"Arrived at {town.Name}. Board travelers, drop passengers, or buy seats at the store.");
        }

        private IEnumerator Hop(string fromId, string toId)
        {
            Vector3 from = _view.TrainPos(fromId);
            Vector3 to = _view.TrainPos(toId);
            Vector3 delta = to - from;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
                _view.Train.rotation = Quaternion.LookRotation(delta);

            float hopSeconds = RailroadGraph.TravelSeconds(RailroadGraph.Distance(fromId, toId));
            float elapsed = 0f;
            var iso = _camera != null ? _camera.GetComponent<IsoCameraController>() : null;
            while (elapsed < hopSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hopSeconds);
                t = t * t * (3f - 2f * t);
                _view.Train.position = Vector3.Lerp(from, to, t);
                if (iso != null)
                    iso.Follow(_view.Train.position);
                yield return null;
            }

            _view.Train.position = to;
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

        private string TravelLabel(string townId)
        {
            var route = RailroadGraph.FindRoute(_session.CurrentTownId, townId);
            int seconds = Mathf.Max(1, Mathf.RoundToInt(RailroadGraph.RouteTravelSeconds(route)));
            return seconds == 1 ? "1 second" : seconds + " seconds";
        }
    }
}
