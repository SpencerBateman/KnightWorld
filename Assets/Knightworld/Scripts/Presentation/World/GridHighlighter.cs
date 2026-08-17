using System.Collections.Generic;
using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class GridHighlighter
    {
        private readonly Transform _root;
        private readonly List<GameObject> _pool = new List<GameObject>();
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
            if (_selection != null)
                _selection.SetActive(false);
        }

        public void ShowReachable(IEnumerable<GridPos> cells)
        {
            foreach (var cell in cells)
                Show(cell, PlaceholderMaterials.Reachable, 0.06f);
        }

        public void ShowPath(IReadOnlyList<GridPos> path)
        {
            if (path == null)
                return;
            for (int i = 1; i < path.Count; i++)
                Show(path[i], PlaceholderMaterials.Path, 0.07f);
        }

        public void ShowHover(GridPos cell)
        {
            Show(cell, PlaceholderMaterials.Hover, 0.08f);
        }

        public void ShowAttack(GridPos cell)
        {
            Show(cell, PlaceholderMaterials.Attack, 0.08f);
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
            }

            _selection.SetActive(true);
            _selection.transform.position = GridWorld.CellCenter(cell, 0.045f);
            _selection.transform.localScale = new Vector3(0.95f, 0.02f, 0.95f);
            _selection.GetComponent<Renderer>().sharedMaterial =
                team == Team.Player ? PlaceholderMaterials.SelectedPlayer : PlaceholderMaterials.SelectedEnemy;
        }

        private void Show(GridPos cell, Material material, float y)
        {
            PlaceholderMaterials.Ensure();
            GameObject quad;
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
                _pool.Add(quad);
            }

            _used++;
            quad.SetActive(true);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.position = GridWorld.CellCenter(cell, y);
            quad.transform.localScale = Vector3.one * (GridWorld.CellSize * 0.9f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
