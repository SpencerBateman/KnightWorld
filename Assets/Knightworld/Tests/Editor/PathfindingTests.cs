using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class PathfindingTests
    {
        [Test]
        public void FindsStraightPathOnOpenGrid()
        {
            var map = new GridMap(5, 5);
            var path = Pathfinder.FindPath(map, new GridPos(0, 0), new GridPos(3, 0), _ => false, _ => false);
            Assert.IsNotNull(path);
            Assert.AreEqual(new GridPos(0, 0), path[0]);
            Assert.AreEqual(new GridPos(3, 0), path[path.Count - 1]);
            Assert.AreEqual(4, path.Count);
        }

        [Test]
        public void PathsAroundUnwalkableWall()
        {
            var map = new GridMap(5, 3);
            map[1, 0].Walkable = false;
            map[1, 1].Walkable = false;
            var path = Pathfinder.FindPath(map, new GridPos(0, 0), new GridPos(2, 0), _ => false, _ => false);
            Assert.IsNotNull(path);
            Assert.IsFalse(path.Contains(new GridPos(1, 0)));
            Assert.IsFalse(path.Contains(new GridPos(1, 1)));
            Assert.AreEqual(new GridPos(2, 0), path[path.Count - 1]);
        }

        [Test]
        public void DoesNotCutCornerThroughWallEdge()
        {
            var map = new GridMap(3, 3);
            map.SetCover(new GridPos(0, 0), Cardinal.East, CoverLevel.Wall);
            map.SetCover(new GridPos(0, 0), Cardinal.North, CoverLevel.Wall);
            Assert.IsFalse(map.CanStep(new GridPos(0, 0), new GridPos(1, 1)));
            Assert.IsNull(Pathfinder.FindPath(map, new GridPos(0, 0), new GridPos(1, 1), _ => false, _ => false));
        }

        [Test]
        public void ReachableRespectsMovementBudget()
        {
            var map = new GridMap(8, 8);
            var reachable = Pathfinder.FindReachable(map, new GridPos(4, 4), 2, _ => false, _ => false);
            Assert.IsFalse(reachable.Contains(new GridPos(4, 4)));
            Assert.IsTrue(reachable.Contains(new GridPos(6, 4)));
            Assert.IsFalse(reachable.Contains(new GridPos(7, 4)));
        }

        [Test]
        public void CannotPathIntoOccupiedGoal()
        {
            var map = new GridMap(5, 5);
            var occupied = new GridPos(2, 0);
            var path = Pathfinder.FindPath(map, new GridPos(0, 0), occupied, _ => false, pos => pos.Equals(occupied));
            Assert.IsNull(path);
        }

        [Test]
        public void PathsAroundTreesAndWalls()
        {
            var map = new GridMap(8, 4);
            Assert.IsTrue(map.PlaceTree(new GridPos(3, 1)));
            Assert.IsFalse(map.IsWalkable(new GridPos(3, 1)));
            Assert.IsFalse(map.IsWalkable(new GridPos(4, 1)));
            Assert.IsFalse(map.IsWalkable(new GridPos(3, 2)));
            Assert.IsFalse(map.IsWalkable(new GridPos(4, 2)));
            var path = Pathfinder.FindPath(map, new GridPos(0, 1), new GridPos(7, 1), _ => false, _ => false);
            Assert.IsNotNull(path);
            Assert.IsFalse(path.Contains(new GridPos(3, 1)));
            Assert.IsFalse(path.Contains(new GridPos(4, 1)));
            Assert.AreEqual(new GridPos(7, 1), path[path.Count - 1]);
        }

        [Test]
        public void WaterTilesBlockMovementButNotSight()
        {
            var map = new GridMap(5, 1);
            map.PlaceWater(new GridPos(2, 0));
            Assert.IsFalse(map.IsWalkable(new GridPos(2, 0)));
            Assert.IsNull(Pathfinder.FindPath(map, new GridPos(0, 0), new GridPos(4, 0), _ => false, _ => false));
            Assert.IsTrue(map.HasLineOfSight(new GridPos(0, 0), new GridPos(4, 0)));
        }

        [Test]
        public void TestDungeonIncludesWater()
        {
            var map = TestDungeon.CreateMap();
            bool hasWater = false;
            for (int x = 0; x < map.Width && !hasWater; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (map[x, y].Feature == CellFeature.Water)
                    {
                        hasWater = true;
                        Assert.IsFalse(map.IsWalkable(new GridPos(x, y)));
                        break;
                    }
                }
            }

            Assert.IsTrue(hasWater);
        }

        [Test]
        public void TreesDoNotBlockLineOfSight()
        {
            var map = new GridMap(6, 3);
            Assert.IsTrue(map.PlaceTree(new GridPos(2, 0)));
            Assert.IsTrue(map.HasLineOfSight(new GridPos(0, 0), new GridPos(5, 0)));
        }

        [Test]
        public void PlacedWallsBlockLineOfSight()
        {
            var map = new GridMap(5, 1);
            map.PlaceWall(new GridPos(2, 0));
            Assert.IsFalse(map.HasLineOfSight(new GridPos(0, 0), new GridPos(4, 0)));
        }

        [Test]
        public void CanPathThroughAllyButNotEnemy()
        {
            var map = new GridMap(5, 1);
            var occupant = new GridPos(1, 0);
            var throughAlly = Pathfinder.FindPath(
                map,
                new GridPos(0, 0),
                new GridPos(2, 0),
                _ => false,
                pos => pos.Equals(occupant));
            Assert.IsNotNull(throughAlly);
            var blockedByEnemy = Pathfinder.FindPath(
                map,
                new GridPos(0, 0),
                new GridPos(2, 0),
                pos => pos.Equals(occupant),
                _ => false);
            Assert.IsNull(blockedByEnemy);
        }
    }
}
