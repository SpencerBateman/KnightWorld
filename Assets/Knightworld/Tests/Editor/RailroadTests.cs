using System;
using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class RailroadTests
    {
        [SetUp]
        public void RestoreDefaultMap()
        {
            RailroadGraph.UseDefault();
        }

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
            Assert.LessOrEqual(millhavenToLakeside, RailroadMap.MaxHopSeconds);
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
        public void NoHopTakesMoreThanTwoMinutes()
        {
            foreach (var from in RailroadGraph.Towns)
            {
                foreach (var link in from.Links)
                {
                    float hop = RailroadGraph.TravelSeconds(RailroadGraph.Distance(from.Id, link));
                    Assert.LessOrEqual(hop, RailroadMap.MaxHopSeconds, from.Id + " to " + link);
                    Assert.GreaterOrEqual(hop, RailroadGraph.MinHopSeconds);
                }
            }

            Assert.AreEqual(RailroadMap.MaxHopSeconds, RailroadGraph.TravelSeconds(1000f));
        }

        [Test]
        public void TravelCountsWallClockTime()
        {
            var session = NewSession();
            var depart = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(session.TryDepart(RailroadGraph.Portmere, depart));
            Assert.IsTrue(session.InTransit);
            Assert.AreEqual(RailroadGraph.Millhaven, session.CurrentTownId);
            float duration = session.TravelDurationSeconds;
            Assert.GreaterOrEqual(duration, 20f);
            Assert.LessOrEqual(duration, 120f);
            Assert.AreEqual(duration - 10f, session.TravelRemainingSeconds(depart.AddSeconds(10f)), 0.02f);
            Assert.IsFalse(session.FinishTravelIfDue(depart.AddSeconds(10f)));
            Assert.IsTrue(session.FinishTravelIfDue(depart.AddSeconds(duration + 1f)));
            Assert.IsFalse(session.InTransit);
            Assert.AreEqual(RailroadGraph.Portmere, session.CurrentTownId);
        }

        [Test]
        public void SaveRemembersATripAfterReload()
        {
            var session = NewSession();
            session.Grant(40);
            var depart = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(session.TryDepart(RailroadGraph.Portmere, depart));
            string text = RailSaveCodec.Write(session.Capture());
            Assert.IsTrue(RailSaveCodec.TryRead(text, out var state));
            var loaded = RailSession.FromSave(state);
            Assert.IsTrue(loaded.InTransit);
            Assert.AreEqual(40, loaded.Score);
            Assert.AreEqual(RailroadGraph.Millhaven, loaded.CurrentTownId);
            Assert.AreEqual(RailroadGraph.Portmere, loaded.TravelToId);
            Assert.IsTrue(loaded.FinishTravelIfDue(depart.AddSeconds(loaded.TravelDurationSeconds + 2f)));
            Assert.AreEqual(RailroadGraph.Portmere, loaded.CurrentTownId);
        }

        [Test]
        public void LockedRouteStaysClosedUntilBoughtAtTheStation()
        {
            RailroadGraph.Use(RailroadMapParser.Parse(@"
start sc
town sc SanClemente
town hidden Hidden
locked sc hidden 5 100
"));
            var session = new RailSession(new SeededRandom(1), "sc");
            Assert.IsTrue(RailroadGraph.AreLinked("sc", "hidden"));
            Assert.IsFalse(session.CanRide("sc", "hidden"));
            Assert.IsFalse(session.TryBuyRoute("hidden"));
            session.Grant(100);
            Assert.IsTrue(session.TryBuyRoute("hidden"));
            Assert.AreEqual(0, session.Score);
            Assert.IsTrue(session.CanRide("sc", "hidden"));
            Assert.IsTrue(session.CanRide("hidden", "sc"));
            Assert.IsFalse(session.TryBuyRoute("hidden"));
        }

        [Test]
        public void LockedRouteCannotBeBoughtFromADifferentTown()
        {
            RailroadGraph.Use(RailroadMapParser.Parse(@"
start ny
town ny NewYork
town sc SanClemente
town hidden Hidden
track ny sc 4
locked sc hidden 5 100
"));
            var session = new RailSession(new SeededRandom(1), "ny");
            session.Grant(100);
            Assert.IsFalse(session.TryBuyRoute("hidden"));
            session.Arrive("sc");
            Assert.IsTrue(session.TryBuyRoute("hidden"));
            Assert.IsTrue(session.CanRide("sc", "hidden"));
        }

        [Test]
        public void PassengersStayOnThePaidNetwork()
        {
            RailroadGraph.Use(RailroadMapParser.Parse(@"
start sc
town sc SanClemente
town open Open
town hidden Hidden
track sc open 4
locked sc hidden 5 100
"));
            var session = new RailSession(new SeededRandom(3), "sc");
            Assert.IsTrue(session.IsAccessible("sc"));
            Assert.IsTrue(session.IsAccessible("open"));
            Assert.IsFalse(session.IsAccessible("hidden"));
            Assert.IsFalse(session.TrySpawnAt("hidden"));
            for (int i = 0; i < RailSession.MaxWaitingPerTown; i++)
            {
                Assert.IsTrue(session.TrySpawnAt("sc"));
                Assert.AreNotEqual("hidden", session.Waiting["sc"][i].DestId);
            }

            session.Waiting["sc"].Clear();
            session.SeedWaiting(2);
            Assert.AreEqual(0, session.Waiting["hidden"].Count);
            session.RollPassengersOnMove();
            Assert.AreEqual(0, session.Waiting["hidden"].Count);

            session.Grant(100);
            Assert.IsTrue(session.TryBuyRoute("hidden"));
            Assert.IsTrue(session.IsAccessible("hidden"));
            Assert.IsTrue(session.TrySpawnAt("hidden"));
            bool wantsHidden = false;
            for (int i = 0; i < 12; i++)
            {
                session.Waiting["sc"].Clear();
                Assert.IsTrue(session.TrySpawnAt("sc"));
                if (session.Waiting["sc"][0].DestId == "hidden")
                    wantsHidden = true;
            }

            Assert.IsTrue(wantsHidden);
        }

        [Test]
        public void TrainCanOnlyUseDirectTracks()
        {
            Assert.IsTrue(RailroadGraph.AreLinked(RailroadGraph.Millhaven, RailroadGraph.Portmere));
            Assert.IsTrue(RailroadGraph.AreLinked(RailroadGraph.Portmere, RailroadGraph.Millhaven));
            Assert.IsFalse(RailroadGraph.AreLinked(RailroadGraph.Millhaven, RailroadGraph.Emberford));
            Assert.IsFalse(RailroadGraph.AreLinked(RailroadGraph.Millhaven, RailroadGraph.Millhaven));
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
        public void SeatUpgradeCostsFiftyAndAddsOneSeat()
        {
            var session = NewSession();
            Assert.AreEqual(2, RailSession.SeatUpgradeStock);
            Assert.IsFalse(session.TryBuySeatUpgrade());
            session.Grant(RailSession.SeatUpgradeCost - 1);
            Assert.IsFalse(session.TryBuySeatUpgrade());
            session.Grant(1);
            Assert.IsTrue(session.TryBuySeatUpgrade());
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(2, session.SeatCount);
            Assert.AreEqual(1, session.SeatUpgradesLeft);
            session.Grant(RailSession.SeatUpgradeCost);
            Assert.IsTrue(session.TryBuySeatUpgrade());
            Assert.AreEqual(3, session.SeatCount);
            Assert.AreEqual(0, session.SeatUpgradesLeft);
            session.Grant(RailSession.SeatUpgradeCost);
            Assert.IsFalse(session.TryBuySeatUpgrade());
            Assert.AreEqual(3, session.SeatCount);
        }

        [Test]
        public void CarriageCostsThreeFiftyAndAddsSixSeatsOnce()
        {
            var session = NewSession();
            Assert.IsFalse(session.TryBuyCarriage());
            session.Grant(RailSession.CarriageCost - 1);
            Assert.IsFalse(session.TryBuyCarriage());
            session.Grant(1);
            Assert.IsTrue(session.TryBuyCarriage());
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(7, session.SeatCount);
            Assert.IsTrue(session.HasCarriage);
            session.Grant(RailSession.CarriageCost);
            Assert.IsFalse(session.TryBuyCarriage());
            Assert.AreEqual(7, session.SeatCount);
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
        public void PassengersRollInWhenTheTrainMoves()
        {
            var session = NewSession();
            for (int i = 0; i < 20; i++)
                session.RollPassengersOnMove();
            Assert.AreEqual(0, session.Waiting[RailroadGraph.Millhaven].Count);
            Assert.Greater(CountWaiting(session), 0);
        }

        [Test]
        public void MoveSpawnRespectsWaitingCap()
        {
            var session = NewSession();
            session.Arrive(RailroadGraph.Millhaven);
            for (int i = 0; i < RailSession.MaxWaitingPerTown; i++)
                Assert.IsTrue(session.TrySpawnAt(RailroadGraph.Portmere));
            for (int i = 0; i < 20; i++)
                session.RollPassengersOnMove();
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

        private static int CountWaiting(RailSession session)
        {
            int count = 0;
            foreach (var town in RailroadGraph.Towns)
                count += session.Waiting[town.Id].Count;
            return count;
        }

        private static void BuySeatsUntil(RailSession session, int seats)
        {
            while (session.SeatCount < seats)
            {
                if (session.SeatUpgradesLeft > 0)
                {
                    session.Grant(RailSession.SeatUpgradeCost);
                    Assert.IsTrue(session.TryBuySeatUpgrade());
                    continue;
                }

                session.Grant(RailSession.CarriageCost);
                Assert.IsTrue(session.TryBuyCarriage());
            }
        }
    }
}
