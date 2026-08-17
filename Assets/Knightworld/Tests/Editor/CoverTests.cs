using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class CoverTests
    {
        [Test]
        public void FacingHalfCoverAddsTwoAc()
        {
            var map = new GridMap(5, 3);
            var defender = new GridPos(3, 1);
            var attacker = new GridPos(1, 1);
            map.SetCover(defender, Cardinal.West, CoverLevel.Half);
            var cover = map.GetCoverAgainst(attacker, defender);
            Assert.AreEqual(CoverLevel.Half, cover);
            Assert.AreEqual(2, CoverRules.ArmorBonus(cover));
        }

        [Test]
        public void ThreeQuarterCoverAddsFiveAc()
        {
            var map = new GridMap(5, 3);
            map.SetCover(new GridPos(3, 1), Cardinal.West, CoverLevel.ThreeQuarter);
            var cover = map.GetCoverAgainst(new GridPos(0, 1), new GridPos(3, 1));
            Assert.AreEqual(CoverLevel.ThreeQuarter, cover);
            Assert.AreEqual(5, CoverRules.ArmorBonus(cover));
        }

        [Test]
        public void WallBetweenUnitsBlocksLineOfSight()
        {
            var map = new GridMap(5, 1);
            map[2, 0].Walkable = false;
            Assert.IsFalse(map.HasLineOfSight(new GridPos(0, 0), new GridPos(4, 0)));
            Assert.AreEqual(CoverLevel.Wall, map.GetCoverAgainst(new GridPos(0, 0), new GridPos(4, 0)));
        }

        [Test]
        public void AdjacentHalfCoverUsesDefenderFacingEdge()
        {
            var map = new GridMap(3, 1);
            map.SetCover(new GridPos(1, 0), Cardinal.West, CoverLevel.Half);
            Assert.AreEqual(CoverLevel.Half, map.GetCoverAgainst(new GridPos(0, 0), new GridPos(1, 0)));
        }

        [Test]
        public void OpenGroundHasNoCover()
        {
            var map = new GridMap(4, 4);
            Assert.AreEqual(CoverLevel.None, map.GetCoverAgainst(new GridPos(0, 0), new GridPos(3, 3)));
            Assert.IsTrue(map.HasLineOfSight(new GridPos(0, 0), new GridPos(3, 3)));
        }
    }
}
