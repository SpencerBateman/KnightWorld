using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class BattlefieldView
    {
        private readonly Transform _root;

        public BattlefieldView(Transform root)
        {
            _root = root;
        }

        public void Build(GridMap map)
        {
            PlaceholderMaterials.Ensure();
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var pos = new GridPos(x, y);
                    var cell = map[pos];
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = cell.Walkable ? $"Tile {pos}" : $"Wall {pos}";
                    tile.transform.SetParent(_root, false);
                    float height = cell.Walkable ? 0.1f : 1.4f;
                    tile.transform.localScale = new Vector3(GridWorld.CellSize * 0.98f, height, GridWorld.CellSize * 0.98f);
                    tile.transform.position = GridWorld.CellCenter(pos, height * 0.5f);
                    var renderer = tile.GetComponent<Renderer>();
                    if (!cell.Walkable)
                        renderer.sharedMaterial = PlaceholderMaterials.Wall;
                    else
                        renderer.sharedMaterial = ((x + y) & 1) == 0 ? PlaceholderMaterials.FloorA : PlaceholderMaterials.FloorB;
                    Object.Destroy(tile.GetComponent<Collider>());
                    DrawCoverEdges(map, pos);
                }
            }
        }

        private void DrawCoverEdges(GridMap map, GridPos pos)
        {
            DrawEdge(map, pos, Cardinal.South);
            DrawEdge(map, pos, Cardinal.West);
            if (pos.Y == map.Height - 1)
                DrawEdge(map, pos, Cardinal.North);
            if (pos.X == map.Width - 1)
                DrawEdge(map, pos, Cardinal.East);
        }

        private void DrawEdge(GridMap map, GridPos pos, Cardinal dir)
        {
            var cover = map[pos].Get(dir);
            if (cover == CoverLevel.None || cover == CoverLevel.Wall)
                return;
            var center = GridWorld.CellCenter(pos, 0f);
            var step = CoverRules.Step(dir);
            var edge = center + new Vector3(step.X, 0f, step.Y) * (GridWorld.CellSize * 0.48f);
            float height = cover == CoverLevel.ThreeQuarter ? 0.85f : 0.45f;
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"{cover} {pos} {dir}";
            box.transform.SetParent(_root, false);
            bool northSouth = dir == Cardinal.North || dir == Cardinal.South;
            box.transform.localScale = northSouth
                ? new Vector3(GridWorld.CellSize * 0.9f, height, 0.12f)
                : new Vector3(0.12f, height, GridWorld.CellSize * 0.9f);
            box.transform.position = edge + Vector3.up * (height * 0.5f);
            box.GetComponent<Renderer>().sharedMaterial =
                cover == CoverLevel.ThreeQuarter ? PlaceholderMaterials.CoverThreeQuarter : PlaceholderMaterials.CoverHalf;
            Object.Destroy(box.GetComponent<Collider>());
        }
    }
}
