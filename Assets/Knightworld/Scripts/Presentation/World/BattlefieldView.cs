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
            DrawGrassField(map);
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var pos = new GridPos(x, y);
                    var cell = map[pos];
                    if (cell.Feature == CellFeature.Water)
                        DrawWaterTile(pos, x, y);
                    else
                        DrawGrassTile(pos, x, y);

                    if (cell.Feature == CellFeature.Wall)
                        DrawWall(pos);
                    else if (cell.Feature == CellFeature.Tree && cell.IsFeatureOrigin)
                        DrawTree(pos);
                    else if (cell.Feature == CellFeature.None)
                        DrawCoverEdges(map, pos);
                }
            }
        }

        private void DrawGrassField(GridMap map)
        {
            var field = GameObject.CreatePrimitive(PrimitiveType.Plane);
            field.name = "GrassField";
            field.transform.SetParent(_root, false);
            var center = GridWorld.MapCenter(map);
            field.transform.position = new Vector3(center.x, GridWorld.FieldY, center.z);
            const float worldSize = 120f;
            field.transform.localScale = new Vector3(worldSize / 10f, 1f, worldSize / 10f);
            var fieldRenderer = field.GetComponent<Renderer>();
            fieldRenderer.sharedMaterial = PlaceholderMaterials.GrassField;
            fieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fieldRenderer.receiveShadows = false;
            Object.Destroy(field.GetComponent<Collider>());
        }

        private void DrawGrassTile(GridPos pos, int x, int y)
        {
            DrawFloor(pos, GridWorld.TileHeight, ((x + y) & 1) == 0 ? PlaceholderMaterials.GrassA : PlaceholderMaterials.GrassB);
        }

        private void DrawWaterTile(GridPos pos, int x, int y)
        {
            DrawFloor(pos, GridWorld.WaterHeight, ((x + y) & 1) == 0 ? PlaceholderMaterials.WaterA : PlaceholderMaterials.WaterB);
        }

        private void DrawFloor(GridPos pos, float height, Material material)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"Tile {pos}";
            tile.transform.SetParent(_root, false);
            tile.transform.localScale = new Vector3(GridWorld.CellSize * 0.98f, height, GridWorld.CellSize * 0.98f);
            tile.transform.position = GridWorld.CellCenter(pos, height * 0.5f);
            var renderer = tile.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = material;
            Object.Destroy(tile.GetComponent<Collider>());
        }

        private void DrawWall(GridPos pos)
        {
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = $"Wall {pos}";
            stone.transform.SetParent(_root, false);
            float height = 1.35f;
            stone.transform.localScale = new Vector3(GridWorld.CellSize * 0.92f, height, GridWorld.CellSize * 0.92f);
            stone.transform.position = GridWorld.CellCenter(pos, GridWorld.TileHeight + height * 0.5f);
            var renderer = stone.GetComponent<Renderer>();
            renderer.sharedMaterial = PlaceholderMaterials.Wall;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Object.Destroy(stone.GetComponent<Collider>());
        }

        private void DrawTree(GridPos pos)
        {
            int seed = pos.X * 37 + pos.Y * 17;
            float lean = ((seed % 5) - 2) * 3f;
            var root = new GameObject($"Tree {pos}").transform;
            root.SetParent(_root, false);
            Vector3 center = GridWorld.CellCenter(pos, 0f) + new Vector3(GridWorld.CellSize * 0.5f, 0f, GridWorld.CellSize * 0.5f);
            root.position = center;
            root.rotation = Quaternion.Euler(0f, seed % 360, 0f);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root, false);
            trunk.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            trunk.transform.localPosition = new Vector3(0f, GridWorld.TileHeight + 0.85f, 0f);
            trunk.transform.localRotation = Quaternion.Euler(lean, 0f, lean * 0.25f);
            StyleProp(trunk, PlaceholderMaterials.Bark);

            var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Canopy";
            leaves.transform.SetParent(root, false);
            leaves.transform.localScale = new Vector3(2.15f, 1.85f, 2.15f);
            leaves.transform.localPosition = new Vector3(0f, GridWorld.TileHeight + 2.15f, 0f);
            StyleProp(leaves, PlaceholderMaterials.Leaves);
        }

        private static void StyleProp(GameObject go, Material material)
        {
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Object.Destroy(go.GetComponent<Collider>());
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
            var center = GridWorld.CellCenter(pos, GridWorld.TileHeight);
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
            var renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial =
                cover == CoverLevel.ThreeQuarter ? PlaceholderMaterials.CoverThreeQuarter : PlaceholderMaterials.CoverHalf;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Object.Destroy(box.GetComponent<Collider>());
        }
    }
}
