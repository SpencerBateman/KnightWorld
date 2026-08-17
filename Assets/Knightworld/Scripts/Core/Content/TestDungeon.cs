using System.Collections.Generic;

namespace Knightworld.Core
{
    public static class TestDungeon
    {
        public const int Width = 12;
        public const int Height = 10;

        public static GridMap CreateMap()
        {
            var map = new GridMap(Width, Height);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    bool edge = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                    map[x, y].Walkable = !edge;
                }
            }

            map.SetCover(new GridPos(4, 3), Cardinal.North, CoverLevel.Half);
            map.SetCover(new GridPos(4, 3), Cardinal.East, CoverLevel.Half);
            map.SetCover(new GridPos(7, 6), Cardinal.West, CoverLevel.Half);
            map.SetCover(new GridPos(7, 6), Cardinal.South, CoverLevel.Half);
            map.SetCover(new GridPos(5, 5), Cardinal.North, CoverLevel.ThreeQuarter);
            return map;
        }

        public static List<UnitState> CreateUnits()
        {
            return new List<UnitState>
            {
                CoreCatalog.Fighter.Instantiate(1, "Aldric", Team.Player, new GridPos(2, 2)),
                CoreCatalog.Wizard.Instantiate(2, "Seraphine", Team.Player, new GridPos(3, 2)),
                CoreCatalog.Goblin.Instantiate(3, "Goblin Scout", Team.Enemy, new GridPos(8, 7)),
                CoreCatalog.Goblin.Instantiate(4, "Goblin Cutthroat", Team.Enemy, new GridPos(9, 6)),
                CoreCatalog.Goblin.Instantiate(5, "Goblin Archer", Team.Enemy, new GridPos(6, 8))
            };
        }
    }
}
