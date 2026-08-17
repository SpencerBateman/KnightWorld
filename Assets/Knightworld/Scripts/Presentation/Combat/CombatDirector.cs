using System.Collections;
using System.Collections.Generic;
using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class CombatDirector : MonoBehaviour
    {
        private CombatSession _session;
        private CombatInput _input;
        private CombatHud _hud;
        private IsoCameraController _camera;
        private GridHighlighter _highlights;
        private readonly Dictionary<int, UnitView> _views = new Dictionary<int, UnitView>();
        private readonly HashSet<int> _moving = new HashSet<int>();

        public void Initialize(
            CombatSession session,
            CombatInput input,
            CombatHud hud,
            IsoCameraController camera,
            GridHighlighter highlights,
            IEnumerable<UnitView> views)
        {
            _session = session;
            _input = input;
            _hud = hud;
            _camera = camera;
            _highlights = highlights;
            _views.Clear();
            foreach (var view in views)
                _views[view.Unit.Id] = view;

            session.TurnStarted += OnTurnStarted;
            session.UnitMoved += OnUnitMoved;
            session.AttackResolved += _ => RefreshViews();
            session.UnitDied += _ => RefreshViews();
            session.CombatEnded += _ =>
            {
                _input.Interactable = false;
                _highlights.Clear();
            };
        }

        private bool IsBusy => _moving.Count > 0;

        private void Update()
        {
            if (_session == null || _highlights == null || _input == null)
                return;
            _highlights.Clear();
            if (_session.Outcome != CombatOutcome.Ongoing)
                return;
            var actor = _session.ActiveUnit;
            if (actor == null || actor.IsDead)
                return;

            _highlights.ShowSelected(actor.Position, actor.Team);
            if (actor.Team != Team.Player || IsBusy)
                return;

            var reachable = _session.GetReachableCells();
            _highlights.ShowReachable(reachable);
            foreach (var enemy in _session.LivingUnits(Team.Enemy))
            {
                if (_session.CanAttack(actor, enemy))
                    _highlights.ShowAttack(enemy.Position);
            }

            if (_input.HoverCell.HasValue)
            {
                var hover = _input.HoverCell.Value;
                if (_input.HoverUnit != null && _input.HoverUnit.Team == Team.Enemy)
                    _highlights.ShowHover(hover);
                else
                {
                    if (reachable.Contains(hover))
                    {
                        var path = _session.GetPathTo(hover);
                        _highlights.ShowPath(path);
                    }

                    _highlights.ShowHover(hover);
                }
            }
        }

        private void OnTurnStarted(UnitState unit)
        {
            RefreshViews();
            if (unit != null)
                _camera.Follow(GridWorld.CellCenter(unit.Position, 0f));
            _hud.Refresh();
            _input.Interactable = false;
            if (unit != null && unit.Team == Team.Enemy && _session.Outcome == CombatOutcome.Ongoing)
                StartCoroutine(RunEnemyTurn());
            else if (unit != null && unit.Team == Team.Player)
                StartCoroutine(EnablePlayerWhenIdle());
        }

        private void OnUnitMoved(UnitState unit, IReadOnlyList<GridPos> path)
        {
            if (unit != null && _views.TryGetValue(unit.Id, out var view) && path != null && path.Count > 1)
            {
                _input.Interactable = false;
                StartCoroutine(AnimateMove(view, path));
            }
            else
            {
                RefreshViews();
            }

            _hud.Refresh();
        }

        private void RefreshViews()
        {
            var activeId = _session.ActiveUnit != null ? _session.ActiveUnit.Id : 0;
            foreach (var pair in _views)
            {
                if (_moving.Contains(pair.Key))
                    continue;
                pair.Value.SnapToGrid();
                pair.Value.RefreshVisual(pair.Key == activeId);
            }
        }

        private IEnumerator RunEnemyTurn()
        {
            _input.Interactable = false;
            yield return WaitUntilIdle();
            yield return new WaitForSeconds(0.35f);
            if (_session.Outcome == CombatOutcome.Ongoing && _session.ActiveUnit != null && _session.ActiveUnit.Team == Team.Enemy)
                SimpleCombatAi.TakeTurn(_session);
            yield return WaitUntilIdle();
            RefreshViews();
        }

        private IEnumerator EnablePlayerWhenIdle()
        {
            yield return WaitUntilIdle();
            if (_session.Outcome == CombatOutcome.Ongoing && _session.ActiveUnit != null && _session.ActiveUnit.Team == Team.Player)
                _input.Interactable = true;
        }

        private IEnumerator WaitUntilIdle()
        {
            while (IsBusy)
                yield return null;
        }

        private IEnumerator AnimateMove(UnitView view, IReadOnlyList<GridPos> path)
        {
            int id = view.Unit.Id;
            _moving.Add(id);
            float standY = view.Unit.IsDead ? GridWorld.TileHeight + 0.15f : GridWorld.UnitY;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 from = GridWorld.CellCenter(path[i - 1], standY);
                Vector3 to = GridWorld.CellCenter(path[i], standY);
                Vector3 delta = to - from;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.0001f)
                    view.transform.rotation = Quaternion.LookRotation(delta);
                float duration = GridWorld.MoveSecondsPerTile;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    t = t * t * (3f - 2f * t);
                    view.transform.position = Vector3.Lerp(from, to, t);
                    _camera.Follow(view.transform.position);
                    yield return null;
                }

                view.transform.position = to;
                if (view.Unit.IsDead)
                    break;
            }

            _moving.Remove(id);
            view.SnapToGrid();
            view.RefreshVisual(_session.ActiveUnit != null && _session.ActiveUnit.Id == id);
            if (_session.Outcome == CombatOutcome.Ongoing && _session.ActiveUnit != null)
                _camera.Follow(GridWorld.CellCenter(_session.ActiveUnit.Position, 0f));
        }
    }
}
