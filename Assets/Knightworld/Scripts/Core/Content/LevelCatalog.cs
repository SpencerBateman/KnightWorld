using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public sealed class LevelSpec
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Blurb { get; }
        public int Seed { get; }
        public Func<GridMap> CreateMap { get; }
        public Func<List<UnitState>> CreateUnits { get; }

        public LevelSpec(string id, string displayName, string blurb, int seed, Func<GridMap> createMap, Func<List<UnitState>> createUnits)
        {
            Id = id;
            DisplayName = displayName;
            Blurb = blurb;
            Seed = seed;
            CreateMap = createMap;
            CreateUnits = createUnits;
        }
    }

    public static class LevelCatalog
    {
        public const string Meadow = "meadow";
        public const string Lakeshore = "lakeshore";
        public const string Ruins = "ruins";

        public static IReadOnlyList<LevelSpec> All { get; } = new[]
        {
            new LevelSpec(Meadow, "Meadow Crossing", "A stream cuts the field. Drive the goblin raiders back.", 17, LevelMaps.CreateMeadow, () => LevelMaps.StandardFight(new GridPos(3, 3), new GridPos(4, 3), new GridPos(19, 13), new GridPos(20, 12), new GridPos(17, 14))),
            new LevelSpec(Lakeshore, "Lakeshore Ambush", "Fight along a deep lake. Watch the banks.", 23, LevelMaps.CreateLakeshore, () => LevelMaps.StandardFight(new GridPos(2, 8), new GridPos(3, 9), new GridPos(20, 8), new GridPos(19, 10), new GridPos(21, 6))),
            new LevelSpec(Ruins, "Ruined Keep", "Broken walls and a moat. Clear the courtyard.", 31, LevelMaps.CreateRuins, () => LevelMaps.StandardFight(new GridPos(4, 4), new GridPos(5, 4), new GridPos(18, 14), new GridPos(20, 13), new GridPos(17, 12)))
        };

        public static LevelSpec Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return All[0];
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == id)
                    return All[i];
            }

            return All[0];
        }
    }
}
