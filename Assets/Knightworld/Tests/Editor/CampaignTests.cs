using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class CampaignTests
    {
        [SetUp]
        public void SetUp()
        {
            CampaignState.Reset();
        }

        [Test]
        public void MeadowStartsUnlockedAndNeighborsStayLocked()
        {
            Assert.IsTrue(CampaignState.IsUnlocked(OverworldGraph.Meadow));
            Assert.IsFalse(CampaignState.IsUnlocked(OverworldGraph.Lakeshore));
            Assert.IsFalse(CampaignState.IsUnlocked(OverworldGraph.Ruins));
        }

        [Test]
        public void CompletingMeadowUnlocksNeighborWorlds()
        {
            CampaignState.PendingLevelId = LevelCatalog.Meadow;
            CampaignState.RecordVictory();
            Assert.IsTrue(CampaignState.Completed.Contains(LevelCatalog.Meadow));
            Assert.IsTrue(CampaignState.IsUnlocked(OverworldGraph.Lakeshore));
            Assert.IsTrue(CampaignState.IsUnlocked(OverworldGraph.Ruins));
        }

        [Test]
        public void RouteWalksThroughClearedHubToOtherWorld()
        {
            CampaignState.PendingLevelId = LevelCatalog.Meadow;
            CampaignState.RecordVictory();
            CampaignState.CurrentNodeId = OverworldGraph.Lakeshore;
            var route = CampaignState.RouteTo(OverworldGraph.Ruins);
            Assert.IsNotNull(route);
            Assert.AreEqual(OverworldGraph.Lakeshore, route[0]);
            Assert.AreEqual(OverworldGraph.Meadow, route[1]);
            Assert.AreEqual(OverworldGraph.Ruins, route[route.Count - 1]);
        }

        [Test]
        public void LockedWorldHasNoRoute()
        {
            Assert.IsNull(CampaignState.RouteTo(OverworldGraph.Lakeshore));
        }

        [Test]
        public void EveryCatalogLevelHasWaterAndWalkableSpawns()
        {
            foreach (var spec in LevelCatalog.All)
            {
                var map = spec.CreateMap();
                Assert.IsTrue(HasWater(map), spec.Id + " should include water");
                foreach (var unit in spec.CreateUnits())
                    Assert.IsTrue(map.IsWalkable(unit.Position), spec.Id + " spawn blocked for " + unit.Name);
            }
        }

        private static bool HasWater(GridMap map)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (map[x, y].Feature == CellFeature.Water)
                        return true;
                }
            }

            return false;
        }
    }
}
