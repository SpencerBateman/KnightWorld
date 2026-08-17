using System.Collections;
using Knightworld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Knightworld.Presentation
{
    public sealed class OverworldController : MonoBehaviour
    {
        public const string SceneName = "Overworld";
        public const string BattleScene = "Battle";
        private const float MarchSeconds = 0.55f;

        private OverworldView _view;
        private OverworldHud _hud;
        private Camera _camera;
        private bool _busy;

        public void Initialize(OverworldView view, OverworldHud hud, Camera worldCamera)
        {
            _view = view;
            _hud = hud;
            _camera = worldCamera;
        }

        private void Update()
        {
            if (_view == null)
                return;
            _view.FaceLabels(_camera);
            if (_busy)
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _hud?.SetTooltip("");
                return;
            }

            if (!TryPickNode(out var nodeId))
            {
                _hud?.SetTooltip("");
                return;
            }

            var node = OverworldGraph.Get(nodeId);
            var level = LevelCatalog.Get(node.LevelId);
            bool unlocked = CampaignState.IsUnlocked(nodeId);
            if (!unlocked)
            {
                _hud?.SetTooltip($"{node.Title} is locked. Clear a neighboring world first.");
            }
            else if (nodeId == CampaignState.CurrentNodeId)
            {
                _hud?.SetTooltip($"{node.Title}\n{level.Blurb}\nClick to enter.");
            }
            else
            {
                _hud?.SetTooltip($"{node.Title}\n{level.Blurb}\nClick to march here.");
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && unlocked)
                StartCoroutine(TravelThenEnter(nodeId));
        }

        private IEnumerator TravelThenEnter(string nodeId)
        {
            _busy = true;
            _hud?.SetTooltip("");
            var route = CampaignState.RouteTo(nodeId);
            if (route == null || route.Count == 0)
            {
                _busy = false;
                yield break;
            }

            if (route.Count > 1)
            {
                for (int i = 1; i < route.Count; i++)
                    yield return March(route[i - 1], route[i]);
            }

            CampaignState.CurrentNodeId = nodeId;
            CampaignState.PendingLevelId = OverworldGraph.Get(nodeId).LevelId;
            _view.RefreshNodes();
            SceneManager.LoadScene(BattleScene);
        }

        private IEnumerator March(string fromId, string toId)
        {
            Vector3 from = _view.TokenPos(fromId);
            Vector3 to = _view.TokenPos(toId);
            Vector3 delta = to - from;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
                _view.Token.rotation = Quaternion.LookRotation(delta);

            float elapsed = 0f;
            while (elapsed < MarchSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / MarchSeconds);
                t = t * t * (3f - 2f * t);
                _view.Token.position = Vector3.Lerp(from, to, t);
                var iso = _camera != null ? _camera.GetComponent<IsoCameraController>() : null;
                if (iso != null)
                    iso.Follow(_view.Token.position);
                yield return null;
            }

            _view.Token.position = to;
        }

        private bool TryPickNode(out string nodeId)
        {
            nodeId = null;
            var camera = _camera != null ? _camera : Camera.main;
            if (camera == null || Mouse.current == null)
                return false;
            var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 120f))
                return false;
            var marker = hit.collider.GetComponentInParent<OverworldNodeMarker>();
            if (marker == null || string.IsNullOrEmpty(marker.NodeId))
                return false;
            nodeId = marker.NodeId;
            return true;
        }
    }
}
