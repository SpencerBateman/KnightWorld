using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public static class GridWorld
    {
        public const float CellSize = 1f;
        public const float TileHeight = 0.25f;
        public const float FieldY = -0.4f;
        public const float HighlightY = 0.28f;
        public const float UnitY = 1.15f;
        public const float MoveSecondsPerTile = 0.13f;

        public static Vector3 CellCenter(GridPos pos, float y = 0f)
        {
            return new Vector3((pos.X + 0.5f) * CellSize, y, (pos.Y + 0.5f) * CellSize);
        }

        public static GridPos WorldToCell(Vector3 world)
        {
            int x = Mathf.FloorToInt(world.x / CellSize);
            int y = Mathf.FloorToInt(world.z / CellSize);
            return new GridPos(x, y);
        }

        public static Vector3 MapCenter(GridMap map)
        {
            return new Vector3(map.Width * CellSize * 0.5f, 0f, map.Height * CellSize * 0.5f);
        }
    }
}
