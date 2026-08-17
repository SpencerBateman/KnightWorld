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
            if (actor.Team != Team.Player)
                return;

            _highlights.ShowReachable(_session.GetReachableCells());
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
                    var path = _session.GetPathTo(hover);
                    if (path != null && _session.GetReachableCells().Contains(hover))
                        _highlights.ShowPath(path);
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
            _input.Interactable = unit != null && unit.Team == Team.Player && _session.Outcome == CombatOutcome.Ongoing;
            if (unit != null && unit.Team == Team.Enemy && _session.Outcome == CombatOutcome.Ongoing)
                StartCoroutine(RunEnemyTurn());
        }

        private void OnUnitMoved(UnitState unit, IReadOnlyList<GridPos> path)
        {
            if (_views.TryGetValue(unit.Id, out var view))
                view.SnapToGrid();
            foreach (var pair in _views)
                pair.Value.SnapToGrid();
            if (unit != null)
                _camera.Follow(GridWorld.CellCenter(unit.Position, 0f));
            _hud.Refresh();
        }

        private void RefreshViews()
        {
            var activeId = _session.ActiveUnit != null ? _session.ActiveUnit.Id : 0;
            foreach (var pair in _views)
            {
                pair.Value.SnapToGrid();
                pair.Value.RefreshVisual(pair.Key == activeId);
            }
        }

        private IEnumerator RunEnemyTurn()
        {
            _input.Interactable = false;
            yield return new WaitForSeconds(0.45f);
            if (_session.Outcome == CombatOutcome.Ongoing && _session.ActiveUnit != null && _session.ActiveUnit.Team == Team.Enemy)
                SimpleCombatAi.TakeTurn(_session);
            RefreshViews();
        }
    }
}
