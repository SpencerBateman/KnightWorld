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
        private const float HopSeconds = 0.7f;
        private const float SpawnSeconds = 7f;

        private RailSession _session;
        private RailroadView _view;
        private RailroadHud _hud;
        private Camera _camera;
        private bool _busy;
        private float _spawnTimer;
        private string _banner;

        public void Initialize(RailSession session, RailroadView view, RailroadHud hud, Camera worldCamera)
        {
            _session = session;
            _view = view;
            _hud = hud;
            _camera = worldCamera;
            _spawnTimer = SpawnSeconds;
            _view.RefreshPassengers(_session);
            _hud.Refresh(_session, null);
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
                        _view.RefreshPassengers(_session);
                    _spawnTimer = SpawnSeconds;
                    _hud.Refresh(_session, _banner);
                }
            }

            if (_busy)
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _hud.SetTooltip("");
                return;
            }

            var mouse = Mouse.current;
            TryHover();
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                HandleClick();
        }

        private void TryHover()
        {
            if (!TryPick(out var passengerId, out var townId))
            {
                _hud.SetTooltip("");
                return;
            }

            if (passengerId.HasValue)
            {
                var person = _session.FindWaiting(passengerId.Value);
                if (person == null)
                {
                    _hud.SetTooltip("That passenger is in another town.");
                    return;
                }

                string dest = RailroadGraph.Get(person.DestId).Name;
                if (_session.FreeSeats <= 0)
                    _hud.SetTooltip($"{person.Name} wants {dest}. Train is full.");
                else
                    _hud.SetTooltip($"{person.Name} → {dest}. Click to board.");
                return;
            }

            var town = RailroadGraph.Get(townId);
            if (town.Id == _session.CurrentTownId)
                _hud.SetTooltip($"You are in {town.Name}.");
            else
                _hud.SetTooltip($"Ride the rails to {town.Name}.");
        }

        private void HandleClick()
        {
            if (!TryPick(out var passengerId, out var townId))
                return;
            if (passengerId.HasValue)
            {
                var person = _session.FindWaiting(passengerId.Value);
                if (person == null)
                    return;
                if (_session.TryBoard(person.Id))
                {
                    _banner = $"{person.Name} boarded for {RailroadGraph.Get(person.DestId).Name}.";
                    _view.RefreshPassengers(_session);
                    _hud.Refresh(_session, _banner);
                }
                else if (_session.FreeSeats <= 0)
                {
                    _banner = "No empty seats.";
                    _hud.Refresh(_session, _banner);
                }

                return;
            }

            if (townId == _session.CurrentTownId)
                return;
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

            int delivered = _session.Arrive(route[route.Count - 1]);
            _view.SnapTrain(_session.CurrentTownId);
            _view.RefreshPassengers(_session);
            var town = RailroadGraph.Get(_session.CurrentTownId);
            _banner = delivered > 0
                ? $"Arrived {town.Name}. +{delivered} point{(delivered == 1 ? "" : "s")}."
                : $"Arrived {town.Name}.";
            _hud.Refresh(_session, _banner);
            _busy = false;
        }

        private IEnumerator Hop(string fromId, string toId)
        {
            Vector3 from = _view.TrainPos(fromId);
            Vector3 to = _view.TrainPos(toId);
            Vector3 delta = to - from;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
                _view.Train.rotation = Quaternion.LookRotation(delta);

            float elapsed = 0f;
            var iso = _camera != null ? _camera.GetComponent<IsoCameraController>() : null;
            while (elapsed < HopSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / HopSeconds);
                t = t * t * (3f - 2f * t);
                _view.Train.position = Vector3.Lerp(from, to, t);
                if (iso != null)
                    iso.Follow(_view.Train.position);
                yield return null;
            }

            _view.Train.position = to;
        }

        private bool TryPick(out int? passengerId, out string townId)
        {
            passengerId = null;
            townId = null;
            var camera = _camera != null ? _camera : Camera.main;
            if (camera == null || Mouse.current == null)
                return false;
            var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 140f);
            float passengerDist = float.MaxValue;
            float townDist = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var passenger = hits[i].collider.GetComponentInParent<PassengerMarker>();
                if (passenger != null && hits[i].distance < passengerDist)
                {
                    passengerId = passenger.PassengerId;
                    passengerDist = hits[i].distance;
                }

                var town = hits[i].collider.GetComponentInParent<TownMarker>();
                if (town != null && hits[i].distance < townDist)
                {
                    townId = town.TownId;
                    townDist = hits[i].distance;
                }
            }

            if (passengerId.HasValue)
            {
                townId = null;
                return true;
            }

            return townId != null;
        }
    }
}
