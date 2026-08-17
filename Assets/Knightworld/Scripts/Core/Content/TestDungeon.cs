using System.Collections.Generic;

namespace Knightworld.Core
{
    public static class TestDungeon
    {
        public const int Width = LevelMaps.Width;
        public const int Height = LevelMaps.Height;

        public static GridMap CreateMap()
        {
            var map = TerrainBrush.Open(Width, Height);
            for (int y = 0; y <= 6; y++)
            {
                TerrainBrush.Water(map, 11, y);
                TerrainBrush.Water(map, 12, y);
            }

            TerrainBrush.Water(map, 13, 6);
            TerrainBrush.Water(map, 14, 6);
            TerrainBrush.Water(map, 15, 6);
            TerrainBrush.Water(map, 15, 7);
            TerrainBrush.Water(map, 16, 7);
            TerrainBrush.Water(map, 17, 7);
            TerrainBrush.Water(map, 16, 8);
            TerrainBrush.Water(map, 17, 8);
            TerrainBrush.Water(map, 18, 8);
            TerrainBrush.Water(map, 16, 9);
            TerrainBrush.Water(map, 17, 9);
            TerrainBrush.Water(map, 18, 7);

            TerrainBrush.WallRun(map, new GridPos(9, 7), new GridPos(13, 7));
            map.PlaceWall(new GridPos(13, 8));
            map.PlaceWall(new GridPos(13, 9));
            TerrainBrush.WallRun(map, new GridPos(4, 12), new GridPos(6, 12));
            TerrainBrush.WallRun(map, new GridPos(18, 9), new GridPos(18, 11));

            map.PlaceTree(new GridPos(1, 1));
            map.PlaceTree(new GridPos(6, 3));
            map.PlaceTree(new GridPos(2, 14));
            map.PlaceTree(new GridPos(14, 12));
            map.PlaceTree(new GridPos(19, 4));
            map.PlaceTree(new GridPos(21, 15));

            map.SetCover(new GridPos(6, 6), Cardinal.North, CoverLevel.Half);
            map.SetCover(new GridPos(6, 6), Cardinal.East, CoverLevel.Half);
            map.SetCover(new GridPos(10, 8), Cardinal.West, CoverLevel.Half);
            map.SetCover(new GridPos(10, 8), Cardinal.South, CoverLevel.Half);
            map.SetCover(new GridPos(12, 10), Cardinal.North, CoverLevel.ThreeQuarter);
            map.SetCover(new GridPos(16, 11), Cardinal.South, CoverLevel.Half);
            map.SetCover(new GridPos(16, 11), Cardinal.West, CoverLevel.Half);
            map.SetCover(new GridPos(8, 13), Cardinal.East, CoverLevel.Half);
            map.SetCover(new GridPos(20, 6), Cardinal.North, CoverLevel.Half);
            return map;
        }

        public static List<UnitState> CreateUnits() =>
            LevelMaps.StandardFight(new GridPos(3, 3), new GridPos(4, 3), new GridPos(19, 13), new GridPos(20, 12), new GridPos(17, 14));
    }
}
