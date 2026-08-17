using Knightworld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Knightworld.Presentation
{
    public sealed class CombatInput : MonoBehaviour
    {
        public CombatSession Session;
        public CombatHud Hud;
        public GridHighlighter Highlights;
        public Camera WorldCamera;

        public GridPos? HoverCell { get; private set; }
        public UnitState HoverUnit { get; private set; }

        public bool Interactable { get; set; } = true;

        private void Update()
        {
            if (Session == null || Session.Outcome != CombatOutcome.Ongoing)
            {
                HoverCell = null;
                HoverUnit = null;
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && Interactable)
            {
                if (Session.ActiveUnit != null && Session.ActiveUnit.Team == Team.Player)
                    Session.EndTurn();
                return;
            }

            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)))
            {
                HoverCell = null;
                HoverUnit = null;
                Hud?.SetTooltip("");
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                HoverCell = null;
                HoverUnit = null;
                Hud?.SetTooltip("");
                return;
            }

            if (!TryPick(out var cell, out var unit))
            {
                HoverCell = null;
                HoverUnit = null;
                Hud?.SetTooltip("");
                return;
            }

            HoverCell = cell;
            HoverUnit = unit;
            UpdateTooltip();

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Interactable)
                HandleClick(cell, unit);
        }

        private void HandleClick(GridPos cell, UnitState unit)
        {
            var actor = Session.ActiveUnit;
            if (actor == null || actor.Team != Team.Player)
                return;

            if (unit != null && unit.Team == Team.Enemy && !unit.IsDead)
            {
                Session.TryAttack(unit.Id);
                return;
            }

            if (unit == null)
                Session.TryMove(cell);
        }

        private void UpdateTooltip()
        {
            if (Hud == null || Session.ActiveUnit == null || Session.ActiveUnit.Team != Team.Player)
            {
                Hud?.SetTooltip("");
                return;
            }

            if (HoverUnit != null && HoverUnit.Team == Team.Enemy && !HoverUnit.IsDead)
            {
                if (Session.TryGetHitChance(HoverUnit.Id, out float chance))
                {
                    var cover = Session.Map.GetCoverAgainst(Session.ActiveUnit.Position, HoverUnit.Position);
                    Hud.SetTooltip($"{Session.ActiveUnit.AttackName} vs {HoverUnit.Name}: {Mathf.RoundToInt(chance * 100f)}%  AC {HoverUnit.ArmorClass} ({CoverRules.Label(cover)})  HP {HoverUnit.Hp}/{HoverUnit.MaxHp}");
                }
                else if (Session.ActiveUnit.HasAction)
                {
                    Hud.SetTooltip($"Cannot attack {HoverUnit.Name} from here.");
                }
                else
                {
                    Hud.SetTooltip("Action already used.");
                }

                return;
            }

            if (HoverCell.HasValue)
            {
                var reachable = Session.GetReachableCells();
                if (reachable.Contains(HoverCell.Value))
                {
                    var path = Session.GetPathTo(HoverCell.Value);
                    int feet = path != null ? (path.Count - 1) * GridMap.FeetPerSquare : 0;
                    Hud.SetTooltip($"Move {feet} ft to {HoverCell.Value}");
                }
                else
                {
                    Hud.SetTooltip("");
                }
            }
        }

        private bool TryPick(out GridPos cell, out UnitState unit)
        {
            cell = default;
            unit = null;
            var camera = WorldCamera != null ? WorldCamera : Camera.main;
            if (camera == null || Mouse.current == null)
                return false;
            var screen = Mouse.current.position.ReadValue();
            var ray = camera.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out var hit, 200f))
            {
                var view = hit.collider.GetComponent<UnitView>();
                if (view != null && view.Unit != null)
                {
                    unit = view.Unit;
                    cell = view.Unit.Position;
                    return true;
                }
            }

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float enter))
                return false;
            cell = GridWorld.WorldToCell(ray.GetPoint(enter));
            if (!Session.Map.InBounds(cell))
                return false;
            unit = Session.UnitAt(cell);
            return true;
        }
    }
}
