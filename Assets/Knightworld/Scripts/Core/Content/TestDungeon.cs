using System.Collections.Generic;

namespace Knightworld.Core
{
    public static class TestDungeon
    {
        public const int Width = 24;
        public const int Height = 18;

        public static GridMap CreateMap()
        {
            var map = new GridMap(Width, Height);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                    map[x, y].Walkable = true;
            }

            PlaceWallRun(map, new GridPos(9, 7), new GridPos(13, 7));
            map.PlaceWall(new GridPos(13, 8));
            map.PlaceWall(new GridPos(13, 9));
            PlaceWallRun(map, new GridPos(4, 12), new GridPos(6, 12));
            PlaceWallRun(map, new GridPos(18, 9), new GridPos(18, 11));

            map.PlaceTree(new GridPos(6, 4));
            map.PlaceTree(new GridPos(7, 8));
            map.PlaceTree(new GridPos(8, 14));
            map.PlaceTree(new GridPos(11, 4));
            map.PlaceTree(new GridPos(11, 11));
            map.PlaceTree(new GridPos(14, 13));
            map.PlaceTree(new GridPos(16, 5));
            map.PlaceTree(new GridPos(20, 7));
            map.PlaceTree(new GridPos(21, 14));
            map.PlaceTree(new GridPos(15, 16));

            map.SetCover(new GridPos(6, 5), Cardinal.North, CoverLevel.Half);
            map.SetCover(new GridPos(6, 5), Cardinal.East, CoverLevel.Half);
            map.SetCover(new GridPos(10, 8), Cardinal.West, CoverLevel.Half);
            map.SetCover(new GridPos(10, 8), Cardinal.South, CoverLevel.Half);
            map.SetCover(new GridPos(12, 10), Cardinal.North, CoverLevel.ThreeQuarter);
            map.SetCover(new GridPos(16, 11), Cardinal.South, CoverLevel.Half);
            map.SetCover(new GridPos(16, 11), Cardinal.West, CoverLevel.Half);
            map.SetCover(new GridPos(8, 12), Cardinal.East, CoverLevel.Half);
            map.SetCover(new GridPos(18, 6), Cardinal.North, CoverLevel.Half);
            return map;
        }

        public static List<UnitState> CreateUnits()
        {
            return new List<UnitState>
            {
                CoreCatalog.Fighter.Instantiate(1, "Aldric", Team.Player, new GridPos(3, 3)),
                CoreCatalog.Wizard.Instantiate(2, "Seraphine", Team.Player, new GridPos(4, 3)),
                CoreCatalog.Goblin.Instantiate(3, "Goblin Scout", Team.Enemy, new GridPos(19, 13)),
                CoreCatalog.Goblin.Instantiate(4, "Goblin Cutthroat", Team.Enemy, new GridPos(20, 12)),
                CoreCatalog.Goblin.Instantiate(5, "Goblin Archer", Team.Enemy, new GridPos(17, 14))
            };
        }

        private static void PlaceWallRun(GridMap map, GridPos from, GridPos to)
        {
            int dx = to.X == from.X ? 0 : (to.X > from.X ? 1 : -1);
            int dy = to.Y == from.Y ? 0 : (to.Y > from.Y ? 1 : -1);
            var pos = from;
            while (true)
            {
                map.PlaceWall(pos);
                if (pos == to)
                    return;
                pos = pos.Offset(dx, dy);
            }
        }
    }
}
