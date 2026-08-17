using System.Collections.Generic;
using Knightworld.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Knightworld.Presentation
{
    public sealed class GridHighlighter
    {
        private readonly Transform _root;
        private readonly List<GameObject> _pool = new List<GameObject>();
        private readonly Dictionary<GridPos, int> _cellToIndex = new Dictionary<GridPos, int>();
        private GameObject _selection;
        private int _used;

        public GridHighlighter(Transform root)
        {
            _root = root;
        }

        public void Clear()
        {
            for (int i = 0; i < _used; i++)
                _pool[i].SetActive(false);
            _used = 0;
            _cellToIndex.Clear();
            if (_selection != null)
                _selection.SetActive(false);
        }

        public void ShowReachable(IEnumerable<GridPos> cells)
        {
            foreach (var cell in cells)
                Show(cell, PlaceholderMaterials.Reachable, GridWorld.CellSize * 0.88f);
        }

        public void ShowPath(IReadOnlyList<GridPos> path)
        {
            if (path == null)
                return;
            for (int i = 1; i < path.Count; i++)
                Show(path[i], PlaceholderMaterials.Path, GridWorld.CellSize * 0.7f);
        }

        public void ShowHover(GridPos cell)
        {
            Show(cell, PlaceholderMaterials.Hover, GridWorld.CellSize * 0.92f);
        }

        public void ShowAttack(GridPos cell)
        {
            Show(cell, PlaceholderMaterials.Attack, GridWorld.CellSize * 0.88f);
        }

        public void ShowSelected(GridPos cell, Team team)
        {
            PlaceholderMaterials.Ensure();
            if (_selection == null)
            {
                _selection = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _selection.name = "Selection";
                _selection.transform.SetParent(_root, false);
                Object.Destroy(_selection.GetComponent<Collider>());
                var renderer = _selection.GetComponent<Renderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            _selection.SetActive(true);
            _selection.transform.position = GridWorld.CellCenter(cell, GridWorld.HighlightY);
            _selection.transform.localScale = new Vector3(0.62f, 0.012f, 0.62f);
            _selection.GetComponent<Renderer>().sharedMaterial =
                team == Team.Player ? PlaceholderMaterials.SelectedPlayer : PlaceholderMaterials.SelectedEnemy;
        }

        private void Show(GridPos cell, Material material, float size)
        {
            PlaceholderMaterials.Ensure();
            GameObject quad;
            if (_cellToIndex.TryGetValue(cell, out int existing))
            {
                quad = _pool[existing];
            }
            else
            {
                if (_used < _pool.Count)
                {
                    quad = _pool[_used];
                }
                else
                {
                    quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "Highlight";
                    quad.transform.SetParent(_root, false);
                    Object.Destroy(quad.GetComponent<Collider>());
                    var renderer = quad.GetComponent<Renderer>();
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    _pool.Add(quad);
                }

                _cellToIndex[cell] = _used;
                _used++;
            }

            quad.SetActive(true);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.position = GridWorld.CellCenter(cell, GridWorld.HighlightY);
            quad.transform.localScale = new Vector3(size, size, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
