using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class RailroadTests
    {
        [Test]
        public void AllTownsAreConnected()
        {
            foreach (var from in RailroadGraph.Towns)
            {
                foreach (var to in RailroadGraph.Towns)
                {
                    var route = RailroadGraph.FindRoute(from.Id, to.Id);
                    Assert.IsNotNull(route, from.Name + " should reach " + to.Name);
                    Assert.AreEqual(from.Id, route[0]);
                    Assert.AreEqual(to.Id, route[route.Count - 1]);
                }
            }
        }

        [Test]
        public void TrainStartsWithTenEmptySeats()
        {
            var session = NewSession();
            Assert.AreEqual(10, RailSession.SeatCount);
            Assert.AreEqual(10, session.FreeSeats);
            Assert.AreEqual(0, session.Score);
        }

        [Test]
        public void BoardingUsesASeatAndLeavingFreesItWithAPoint()
        {
            var session = NewSession();
            Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Millhaven));
            var rider = session.WaitingHere[0];
            string dest = rider.DestId;
            Assert.IsTrue(session.TryBoard(rider.Id));
            Assert.AreEqual(9, session.FreeSeats);
            Assert.AreEqual(1, session.Arrive(dest));
            Assert.AreEqual(1, session.Score);
            Assert.AreEqual(10, session.FreeSeats);
            Assert.AreEqual(0, session.Onboard.Count);
        }

        [Test]
        public void CannotBoardAnEleventhPassenger()
        {
            var session = NewSession();
            for (int i = 0; i < RailSession.SeatCount; i++)
            {
                session.Waiting[RailroadGraph.Millhaven].Clear();
                Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Millhaven));
                Assert.IsTrue(session.TryBoard(session.WaitingHere[0].Id));
            }

            session.Waiting[RailroadGraph.Millhaven].Clear();
            Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Millhaven));
            Assert.IsFalse(session.TryBoard(session.WaitingHere[0].Id));
            Assert.AreEqual(RailSession.SeatCount, session.Onboard.Count);
        }

        [Test]
        public void CannotBoardSomeoneWaitingInAnotherTown()
        {
            var session = NewSession();
            Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Lakeside));
            var other = session.Waiting[RailroadGraph.Lakeside][0];
            Assert.IsFalse(session.TryBoard(other.Id));
        }

        [Test]
        public void ArrivingDoesNotDropPassengersForOtherTowns()
        {
            var session = NewSession();
            Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Millhaven));
            var rider = session.WaitingHere[0];
            Assert.IsTrue(session.TryBoard(rider.Id));
            int delivered = session.Arrive(RailroadGraph.Millhaven);
            Assert.AreEqual(0, delivered);
            Assert.AreEqual(1, session.Onboard.Count);
        }

        [Test]
        public void WaitingIsCappedPerTown()
        {
            var session = NewSession();
            for (int i = 0; i < RailSession.MaxWaitingPerTown; i++)
                Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Portmere));
            Assert.IsFalse(session.TrySpawnAt(RailroadGraph.Portmere));
            Assert.AreEqual(RailSession.MaxWaitingPerTown, session.Waiting[RailroadGraph.Portmere].Count);
        }

        [Test]
        public void SeedPlacesPeopleAtEveryTown()
        {
            var session = NewSession();
            session.SeedWaiting(2);
            foreach (var town in RailroadGraph.Towns)
            {
                Assert.AreEqual(2, session.Waiting[town.Id].Count);
                foreach (var person in session.Waiting[town.Id])
                    Assert.AreNotEqual(town.Id, person.DestId);
            }
        }

        private static RailSession NewSession()
        {
            return new RailSession(new SeededRandom(7), RailroadGraph.Millhaven);
        }
    }
}
