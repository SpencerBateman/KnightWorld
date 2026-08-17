using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class AttackResolverTests
    {
        [Test]
        public void HitWhenTotalMeetsArmorClass()
        {
            var map = new GridMap(3, 1);
            var attacker = MakeUnit(1, Team.Player, new GridPos(0, 0), attackBonus: 5, ac: 10);
            var defender = MakeUnit(2, Team.Enemy, new GridPos(1, 0), attackBonus: 0, ac: 13);
            var rng = new SequenceRandom(8, 4);
            var result = AttackResolver.Resolve(attacker, defender, map, rng);
            Assert.AreEqual(8, result.D20);
            Assert.AreEqual(13, result.TotalToHit);
            Assert.IsTrue(result.Hit);
            Assert.AreEqual(7, result.Damage);
        }

        [Test]
        public void NaturalOneAlwaysMisses()
        {
            var map = new GridMap(3, 1);
            var attacker = MakeUnit(1, Team.Player, new GridPos(0, 0), attackBonus: 20, ac: 10);
            var defender = MakeUnit(2, Team.Enemy, new GridPos(1, 0), attackBonus: 0, ac: 10);
            var result = AttackResolver.Resolve(attacker, defender, map, new SequenceRandom(1, 6));
            Assert.IsFalse(result.Hit);
            Assert.AreEqual(0, result.Damage);
        }

        [Test]
        public void NaturalTwentyAlwaysHitsAndCrits()
        {
            var map = new GridMap(3, 1);
            var attacker = MakeUnit(1, Team.Player, new GridPos(0, 0), attackBonus: 0, ac: 10);
            var defender = MakeUnit(2, Team.Enemy, new GridPos(1, 0), attackBonus: 0, ac: 30);
            var result = AttackResolver.Resolve(attacker, defender, map, new SequenceRandom(20, 3, 5));
            Assert.IsTrue(result.Hit);
            Assert.IsTrue(result.Critical);
            Assert.AreEqual(11, result.Damage);
        }

        [Test]
        public void CoverRaisesTargetAc()
        {
            var map = new GridMap(4, 1);
            map.SetCover(new GridPos(3, 0), Cardinal.West, CoverLevel.Half);
            var attacker = MakeUnit(1, Team.Player, new GridPos(0, 0), attackBonus: 5, ac: 10);
            var defender = MakeUnit(2, Team.Enemy, new GridPos(3, 0), attackBonus: 0, ac: 13);
            var result = AttackResolver.Resolve(attacker, defender, map, new SequenceRandom(9, 4));
            Assert.AreEqual(2, result.CoverBonus);
            Assert.AreEqual(15, result.TargetAc);
            Assert.IsFalse(result.Hit);
        }

        [Test]
        public void HitChanceCountsNaturalTwentyAndOne()
        {
            var map = new GridMap(2, 1);
            var attacker = MakeUnit(1, Team.Player, new GridPos(0, 0), attackBonus: 0, ac: 10);
            var defender = MakeUnit(2, Team.Enemy, new GridPos(1, 0), attackBonus: 0, ac: 21);
            float chance = AttackResolver.EstimateHitChance(attacker, defender, map);
            Assert.AreEqual(0.05f, chance);
        }

        private static UnitState MakeUnit(int id, Team team, GridPos pos, int attackBonus, int ac)
        {
            return new UnitState(
                id,
                "Unit" + id,
                "Test",
                team,
                pos,
                10,
                ac,
                30,
                0,
                attackBonus,
                5,
                new DiceFormula(1, 8, 3),
                "Slash");
        }
    }
}
