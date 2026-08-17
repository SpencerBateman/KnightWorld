using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class OpportunityAttackTests
    {
        [Test]
        public void LeavingReachConsumesReactionAndAttacks()
        {
            var map = new GridMap(8, 3);
            var fighter = MakeUnit(1, "Aldric", Team.Player, new GridPos(0, 1), 20, 16, 5, 60);
            var goblin = MakeUnit(2, "Goblin", Team.Enemy, new GridPos(1, 1), 7, 15, 4, 30);
            var session = new CombatSession(map, new[] { fighter, goblin }, new SequenceRandom(20, 1, 15, 3));
            session.Start();
            Assert.AreEqual(1, session.ActiveUnit.Id);
            Assert.IsTrue(goblin.HasReaction);

            var result = session.TryMove(new GridPos(4, 1));
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.OpportunityAttacks.Count);
            Assert.IsFalse(goblin.HasReaction);
            Assert.AreEqual(15, result.OpportunityAttacks[0].D20);
            Assert.IsTrue(result.OpportunityAttacks[0].Opportunity);
        }

        [Test]
        public void SecondLeaveDoesNotTriggerWithoutReaction()
        {
            var map = new GridMap(10, 3);
            var fighter = MakeUnit(1, "Aldric", Team.Player, new GridPos(0, 1), 30, 16, 5, 60);
            var goblin = MakeUnit(2, "Goblin", Team.Enemy, new GridPos(1, 1), 7, 15, 4, 30);
            var session = new CombatSession(map, new[] { fighter, goblin }, new SequenceRandom(20, 1, 10, 4));
            session.Start();
            var first = session.TryMove(new GridPos(4, 1));
            Assert.AreEqual(1, first.OpportunityAttacks.Count);
            Assert.IsFalse(goblin.HasReaction);
            var second = session.TryMove(new GridPos(6, 1));
            Assert.IsTrue(second.Success);
            Assert.AreEqual(0, second.OpportunityAttacks.Count);
        }

        [Test]
        public void NoOpportunityIfReactionAlreadySpent()
        {
            var map = new GridMap(8, 3);
            var fighter = MakeUnit(1, "Aldric", Team.Player, new GridPos(0, 1), 20, 16, 5, 60);
            var goblin = MakeUnit(2, "Goblin", Team.Enemy, new GridPos(1, 1), 7, 15, 4, 30);
            var session = new CombatSession(map, new[] { fighter, goblin }, new SequenceRandom(20, 1));
            session.Start();
            goblin.HasReaction = false;
            var result = session.TryMove(new GridPos(4, 1));
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.OpportunityAttacks.Count);
        }

        private static UnitState MakeUnit(int id, string name, Team team, GridPos pos, int hp, int ac, int attackBonus, int speedFeet)
        {
            return new UnitState(
                id,
                name,
                "Test",
                team,
                pos,
                hp,
                ac,
                speedFeet,
                0,
                attackBonus,
                5,
                new DiceFormula(1, 6, 2),
                "Scimitar");
        }
    }
}
