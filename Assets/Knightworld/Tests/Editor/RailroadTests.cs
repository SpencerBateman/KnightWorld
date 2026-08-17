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
        public void NetworkHasTenTowns()
        {
            Assert.AreEqual(10, RailroadGraph.Towns.Count);
        }

        [Test]
        public void LongerTrackTakesMoreTravelTime()
        {
            float millhavenToPortmere = RailroadGraph.TravelSeconds(RailroadGraph.Distance(RailroadGraph.Millhaven, RailroadGraph.Portmere));
            float millhavenToLakeside = RailroadGraph.TravelSeconds(RailroadGraph.Distance(RailroadGraph.Millhaven, RailroadGraph.Lakeside));
            Assert.Greater(RailroadGraph.Distance(RailroadGraph.Millhaven, RailroadGraph.Lakeside), RailroadGraph.Distance(RailroadGraph.Millhaven, RailroadGraph.Portmere));
            Assert.Greater(millhavenToLakeside, millhavenToPortmere);
        }

        [Test]
        public void RoutePrefersShorterTrackToEmberford()
        {
            var route = RailroadGraph.FindRoute(RailroadGraph.Millhaven, RailroadGraph.Emberford);
            Assert.AreEqual(RailroadGraph.Portmere, route[1]);
            float viaPortmere = RailroadGraph.RouteDistance(route);
            float viaLakeside = RailroadGraph.Distance(RailroadGraph.Millhaven, RailroadGraph.Lakeside)
                                + RailroadGraph.Distance(RailroadGraph.Lakeside, RailroadGraph.Emberford);
            Assert.Less(viaPortmere, viaLakeside);
        }

        [Test]
        public void TrainStartsWithOneEmptySeat()
        {
            var session = NewSession();
            Assert.AreEqual(1, RailSession.StartingSeats);
            Assert.AreEqual(1, session.SeatCount);
            Assert.AreEqual(1, session.FreeSeats);
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
            Assert.AreEqual(0, session.FreeSeats);
            session.Arrive(dest);
            Assert.IsTrue(session.TryAlight(rider.Id, out bool scored));
            Assert.IsTrue(scored);
            Assert.AreEqual(rider.Fare, session.Score);
            Assert.Greater(rider.Fare, 0);
            Assert.AreEqual(1, session.FreeSeats);
            Assert.AreEqual(0, session.Onboard.Count);
        }

        [Test]
        public void CannotBoardWhenFull()
        {
            var session = NewSession();
            for (int i = 0; i < session.SeatCount; i++)
            {
                session.Waiting[RailroadGraph.Millhaven].Clear();
                Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Millhaven));
                Assert.IsTrue(session.TryBoard(session.WaitingHere[0].Id));
            }

            session.Waiting[RailroadGraph.Millhaven].Clear();
            Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Millhaven));
            Assert.IsFalse(session.TryBoard(session.WaitingHere[0].Id));
            Assert.AreEqual(session.SeatCount, session.Onboard.Count);
        }

        [Test]
        public void SeatUpgradeCostsFiftyAndAddsTwoSeats()
        {
            var session = NewSession();
            Assert.IsFalse(session.TryBuySeatUpgrade());
            session.Grant(RailSession.SeatUpgradeCost - 1);
            Assert.IsFalse(session.TryBuySeatUpgrade());
            session.Grant(1);
            Assert.IsTrue(session.TryBuySeatUpgrade());
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(3, session.SeatCount);
            Assert.AreEqual(3, session.FreeSeats);
            session.Grant(RailSession.SeatUpgradeCost);
            Assert.IsTrue(session.TryBuySeatUpgrade());
            Assert.AreEqual(5, session.SeatCount);
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
            session.Arrive(RailroadGraph.Millhaven);
            Assert.AreEqual(1, session.Onboard.Count);
            Assert.IsTrue(session.TryAlight(rider.Id, out bool scored));
            Assert.IsFalse(scored);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0, session.Onboard.Count);
            Assert.AreEqual(1, session.WaitingHere.Count);
        }

        [Test]
        public void CannotDropOffSomeoneWhoIsNotOnBoard()
        {
            var session = NewSession();
            Assert.IsFalse(session.TryAlight(99, out bool scored));
            Assert.IsFalse(scored);
        }

        [Test]
        public void ArrivingDoesNotAutomaticallyDropPassengers()
        {
            var session = NewSession();
            Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Millhaven));
            var rider = session.WaitingHere[0];
            string dest = rider.DestId;
            Assert.IsTrue(session.TryBoard(rider.Id));
            session.Arrive(dest);
            Assert.AreEqual(1, session.Onboard.Count);
            Assert.AreEqual(0, session.Score);
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

        [Test]
        public void DestinationCountsFollowOnboardPassengers()
        {
            var session = NewSession();
            Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Millhaven));
            var rider = session.WaitingHere[0];
            Assert.AreEqual(0, session.CountOnboardTo(rider.DestId));
            Assert.IsTrue(session.TryBoard(rider.Id));
            Assert.AreEqual(1, session.CountOnboardTo(rider.DestId));
        }

        [Test]
        public void DestinationTalliesAreSortedByCount()
        {
            var session = NewSession();
            BuySeatsUntil(session, 3);
            session.Waiting[RailroadGraph.Millhaven].Add(new Passenger(1, "Ada", RailroadGraph.Millhaven, RailroadGraph.Lakeside));
            session.Waiting[RailroadGraph.Millhaven].Add(new Passenger(2, "Bram", RailroadGraph.Millhaven, RailroadGraph.Lakeside));
            session.Waiting[RailroadGraph.Millhaven].Add(new Passenger(3, "Cora", RailroadGraph.Millhaven, RailroadGraph.Hillcrest));
            Assert.IsTrue(session.TryBoard(1));
            Assert.IsTrue(session.TryBoard(2));
            Assert.IsTrue(session.TryBoard(3));
            var tallies = session.DestinationTallies();
            Assert.AreEqual(2, tallies.Count);
            Assert.AreEqual(RailroadGraph.Lakeside, tallies[0].TownId);
            Assert.AreEqual(2, tallies[0].Count);
            Assert.AreEqual(RailroadGraph.Hillcrest, tallies[1].TownId);
            Assert.AreEqual(1, tallies[1].Count);
        }

        [Test]
        public void DropOffPassengersAreListedFirst()
        {
            var session = NewSession();
            BuySeatsUntil(session, 2);
            session.Waiting[RailroadGraph.Millhaven].Add(new Passenger(1, "Ada", RailroadGraph.Millhaven, RailroadGraph.Lakeside));
            session.Waiting[RailroadGraph.Millhaven].Add(new Passenger(2, "Bram", RailroadGraph.Millhaven, RailroadGraph.Hillcrest));
            Assert.IsTrue(session.TryBoard(1));
            Assert.IsTrue(session.TryBoard(2));
            session.Arrive(RailroadGraph.Hillcrest);
            var ordered = session.OnboardReadyFirst();
            Assert.AreEqual(2, ordered[0].Id);
            Assert.AreEqual(RailroadGraph.Hillcrest, ordered[0].DestId);
            Assert.AreEqual(1, ordered[1].Id);
        }

        [Test]
        public void FareScalesWithTrackDistance()
        {
            int shortHop = Passenger.FareBetween(RailroadGraph.Millhaven, RailroadGraph.Portmere);
            int longHop = Passenger.FareBetween(RailroadGraph.Willowgate, RailroadGraph.Saltmarsh);
            Assert.Greater(shortHop, 0);
            Assert.Greater(longHop, shortHop);
        }

        [Test]
        public void EarlyDropOffRecalculatesRemainingFare()
        {
            var session = NewSession();
            session.Waiting[RailroadGraph.Millhaven].Add(new Passenger(1, "Ada", RailroadGraph.Millhaven, RailroadGraph.Northspire));
            int original = session.WaitingHere[0].Fare;
            Assert.IsTrue(session.TryBoard(1));
            session.Arrive(RailroadGraph.Portmere);
            Assert.IsTrue(session.TryAlight(1, out bool scored));
            Assert.IsFalse(scored);
            var waiting = session.WaitingHere[0];
            Assert.AreEqual(Passenger.FareBetween(RailroadGraph.Portmere, RailroadGraph.Northspire), waiting.Fare);
            Assert.Less(waiting.Fare, original);
        }

        private static RailSession NewSession()
        {
            return new RailSession(new SeededRandom(7), RailroadGraph.Millhaven);
        }

        private static void BuySeatsUntil(RailSession session, int seats)
        {
            while (session.SeatCount < seats)
            {
                session.Grant(RailSession.SeatUpgradeCost);
                Assert.IsTrue(session.TryBuySeatUpgrade());
            }
        }
    }
}
