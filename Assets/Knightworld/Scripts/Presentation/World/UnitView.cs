using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class UnitView : MonoBehaviour
    {
        public UnitState Unit { get; private set; }
        private Renderer _renderer;

        public static UnitView Spawn(UnitState unit, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = unit.Name;
            go.transform.SetParent(parent, false);
            go.transform.position = GridWorld.CellCenter(unit.Position, 1f);
            go.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
            var view = go.AddComponent<UnitView>();
            view.Unit = unit;
            view._renderer = go.GetComponent<Renderer>();
            view.RefreshVisual(false);
            return view;
        }

        public void SnapToGrid()
        {
            if (Unit == null)
                return;
            transform.position = GridWorld.CellCenter(Unit.Position, Unit.IsDead ? 0.35f : 1f);
            if (Unit.IsDead)
                transform.localScale = new Vector3(0.9f, 0.2f, 0.55f);
            RefreshVisual(false);
        }

        public void RefreshVisual(bool isActive)
        {
            PlaceholderMaterials.Ensure();
            if (Unit == null || _renderer == null)
                return;
            if (Unit.IsDead)
                _renderer.sharedMaterial = PlaceholderMaterials.Dead;
            else if (isActive)
                _renderer.sharedMaterial = PlaceholderMaterials.Active;
            else
                _renderer.sharedMaterial = Unit.Team == Team.Player ? PlaceholderMaterials.Player : PlaceholderMaterials.Enemy;
        }
    }
}
