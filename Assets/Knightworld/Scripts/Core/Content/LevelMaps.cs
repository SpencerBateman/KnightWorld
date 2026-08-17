using System.Collections.Generic;

namespace Knightworld.Core
{
    public static class LevelMaps
    {
        public const int Width = 24;
        public const int Height = 18;

        public static GridMap CreateMeadow() => TestDungeon.CreateMap();

        public static GridMap CreateLakeshore()
        {
            var map = TerrainBrush.Open(Width, Height);
            TerrainBrush.WaterRect(map, 6, 5, 16, 12);
            TerrainBrush.Water(map, 5, 8);
            TerrainBrush.Water(map, 5, 9);
            TerrainBrush.Water(map, 17, 8);
            TerrainBrush.Water(map, 17, 9);
            TerrainBrush.Water(map, 10, 4);
            TerrainBrush.Water(map, 11, 4);
            map.PlaceTree(new GridPos(1, 14));
            map.PlaceTree(new GridPos(3, 3));
            map.PlaceTree(new GridPos(20, 13));
            map.PlaceTree(new GridPos(21, 1));
            TerrainBrush.WallRun(map, new GridPos(2, 11), new GridPos(4, 11));
            TerrainBrush.WallRun(map, new GridPos(19, 4), new GridPos(21, 4));
            map.SetCover(new GridPos(3, 7), Cardinal.East, CoverLevel.Half);
            map.SetCover(new GridPos(20, 7), Cardinal.West, CoverLevel.Half);
            map.SetCover(new GridPos(8, 14), Cardinal.South, CoverLevel.ThreeQuarter);
            map.SetCover(new GridPos(15, 3), Cardinal.North, CoverLevel.Half);
            return map;
        }

        public static GridMap CreateRuins()
        {
            var map = TerrainBrush.Open(Width, Height);
            TerrainBrush.WaterRect(map, 0, 0, 23, 1);
            TerrainBrush.WaterRect(map, 0, 0, 1, 17);
            TerrainBrush.Water(map, 22, 2);
            TerrainBrush.Water(map, 22, 3);
            TerrainBrush.WallRun(map, new GridPos(8, 6), new GridPos(15, 6));
            TerrainBrush.WallRun(map, new GridPos(8, 6), new GridPos(8, 12));
            TerrainBrush.WallRun(map, new GridPos(15, 6), new GridPos(15, 12));
            TerrainBrush.WallRun(map, new GridPos(8, 12), new GridPos(10, 12));
            TerrainBrush.WallRun(map, new GridPos(13, 12), new GridPos(15, 12));
            map.PlaceTree(new GridPos(3, 14));
            map.PlaceTree(new GridPos(19, 3));
            map.PlaceTree(new GridPos(20, 15));
            map.SetCover(new GridPos(10, 8), Cardinal.North, CoverLevel.Half);
            map.SetCover(new GridPos(13, 9), Cardinal.West, CoverLevel.ThreeQuarter);
            map.SetCover(new GridPos(11, 14), Cardinal.South, CoverLevel.Half);
            map.SetCover(new GridPos(6, 9), Cardinal.East, CoverLevel.Half);
            return map;
        }

        public static List<UnitState> StandardFight(GridPos fighter, GridPos wizard, GridPos goblinA, GridPos goblinB, GridPos goblinC)
        {
            return new List<UnitState>
            {
                CoreCatalog.Fighter.Instantiate(1, "Aldric", Team.Player, fighter),
                CoreCatalog.Wizard.Instantiate(2, "Seraphine", Team.Player, wizard),
                CoreCatalog.Goblin.Instantiate(3, "Goblin Scout", Team.Enemy, goblinA),
                CoreCatalog.Goblin.Instantiate(4, "Goblin Cutthroat", Team.Enemy, goblinB),
                CoreCatalog.Goblin.Instantiate(5, "Goblin Archer", Team.Enemy, goblinC)
            };
        }
    }
}
