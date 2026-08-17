namespace Knightworld.Core
{
    public static class TerrainBrush
    {
        public static GridMap Open(int width, int height)
        {
            var map = new GridMap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    map[x, y].Walkable = true;
            }

            return map;
        }

        public static void WallRun(GridMap map, GridPos from, GridPos to)
        {
            int dx = to.X == from.X ? 0 : (to.X > from.X ? 1 : -1);
            int dy = to.Y == from.Y ? 0 : (to.Y > from.Y ? 1 : -1);
            var pos = from;
            while (true)
            {
                if (map[pos].Feature == CellFeature.None)
                    map.PlaceWall(pos);
                if (pos == to)
                    return;
                pos = pos.Offset(dx, dy);
            }
        }

        public static void Water(GridMap map, int x, int y) => map.PlaceWater(new GridPos(x, y));

        public static void WaterRect(GridMap map, int x0, int y0, int x1, int y1)
        {
            int minX = x0 < x1 ? x0 : x1;
            int maxX = x0 > x1 ? x0 : x1;
            int minY = y0 < y1 ? y0 : y1;
            int maxY = y0 > y1 ? y0 : y1;
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                    map.PlaceWater(new GridPos(x, y));
            }
        }
    }
}
