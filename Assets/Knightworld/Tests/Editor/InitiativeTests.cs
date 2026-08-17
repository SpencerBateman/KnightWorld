using System.Collections.Generic;
using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class InitiativeTests
    {
        [Test]
        public void HigherTotalActsFirst()
        {
            var a = MakeUnit(1, 2);
            var b = MakeUnit(2, 2);
            var queue = new InitiativeQueue();
            queue.Roll(new List<UnitState> { a, b }, new SequenceRandom(10, 18));
            Assert.AreEqual(2, queue.CurrentUnitId);
            Assert.AreEqual(1, queue.Round);
        }

        [Test]
        public void EndTurnAdvancesToNextLivingUnit()
        {
            var map = new GridMap(4, 4);
            var fighter = MakeCombatant(1, Team.Player, new GridPos(0, 0));
            var goblin = MakeCombatant(2, Team.Enemy, new GridPos(2, 2));
            var session = new CombatSession(map, new[] { fighter, goblin }, new SequenceRandom(20, 1));
            session.Start();
            Assert.AreEqual(1, session.ActiveUnit.Id);
            session.EndTurn();
            Assert.AreEqual(2, session.ActiveUnit.Id);
            session.EndTurn();
            Assert.AreEqual(1, session.ActiveUnit.Id);
            Assert.AreEqual(2, session.Initiative.Round);
        }

        [Test]
        public void DeadUnitsAreSkipped()
        {
            var map = new GridMap(4, 4);
            var fighter = MakeCombatant(1, Team.Player, new GridPos(0, 0));
            var goblin = MakeCombatant(2, Team.Enemy, new GridPos(2, 2));
            var goblin2 = MakeCombatant(3, Team.Enemy, new GridPos(3, 3));
            var session = new CombatSession(map, new[] { fighter, goblin, goblin2 }, new SequenceRandom(20, 10, 1));
            session.Start();
            Assert.AreEqual(1, session.ActiveUnit.Id);
            goblin.Hp = 0;
            session.EndTurn();
            Assert.AreEqual(3, session.ActiveUnit.Id);
        }

        private static UnitState MakeUnit(int id, int bonus)
        {
            return MakeCombatant(id, Team.Player, new GridPos(id, 0), bonus);
        }

        private static UnitState MakeCombatant(int id, Team team, GridPos pos, int initiativeBonus = 0)
        {
            return new UnitState(
                id,
                "Unit" + id,
                "Test",
                team,
                pos,
                10,
                12,
                30,
                initiativeBonus,
                4,
                5,
                new DiceFormula(1, 6, 0),
                "Hit");
        }
    }
}
