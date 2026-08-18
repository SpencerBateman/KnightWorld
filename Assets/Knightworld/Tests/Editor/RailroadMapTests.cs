using System;
using Knightworld.Core;
using NUnit.Framework;

namespace Knightworld.Tests
{
    public sealed class RailroadMapTests
    {
        [TearDown]
        public void RestoreDefaultMap()
        {
            RailroadGraph.UseDefault();
        }

        [Test]
        public void ParsesTownsTracksAndTitle()
        {
            var map = RailroadMapParser.Parse(@"
# a tiny loop
title Loop Line
start a
town a Alpha
town b ""Beta Town""
town c Gamma
track a b
track b c 12
track c a
landmark lake b
");
            Assert.AreEqual("Loop Line", map.Title);
            Assert.AreEqual("a", map.StartTownId);
            Assert.AreEqual(3, map.Towns.Count);
            Assert.AreEqual("Beta Town", map.Get("b").Name);
            Assert.AreEqual(2, map.Get("a").Links.Count);
            Assert.AreEqual(RailroadMapLayout.DefaultTrackLength, map.Distance("a", "b"), 0.001f);
            Assert.AreEqual(12f, map.Distance("b", "c"), 0.001f);
            Assert.AreEqual(1, map.Landmarks.Count);
            Assert.AreEqual(LandmarkDef.Lake, map.Landmarks[0].Kind);
            Assert.IsNotNull(map.FindRoute("a", "c"));
        }

        [Test]
        public void TownLineCanListNeighbors()
        {
            var map = RailroadMapParser.Parse(@"
town millhaven Millhaven lakeside
town lakeside Lakeside
");
            Assert.AreEqual(RailroadMapLayout.DefaultTrackLength, map.Distance("millhaven", "lakeside"), 0.001f);
            Assert.AreEqual(1, map.Get("millhaven").Links.Count);
            Assert.AreEqual("lakeside", map.Get("millhaven").Links[0]);
        }

        [Test]
        public void TrackLengthSetsFareAndMapSpacing()
        {
            var map = RailroadMapParser.Parse(@"
start west
town west West
town east East
track west east 50
");
            RailroadGraph.Use(map);
            Assert.AreEqual(50f, RailroadGraph.Distance("west", "east"), 0.001f);
            Assert.AreEqual(50, Passenger.FareBetween("west", "east"));
            Assert.AreEqual(50f, VisualDistance(map.Get("west"), map.Get("east")), 0.05f);
        }

        [Test]
        public void LongerTracksSitFartherApart()
        {
            var map = RailroadMapParser.Parse(@"
start hub
town hub Hub
town near Near
town far Far
track hub near 6
track hub far 18
");
            float near = VisualDistance(map.Get("hub"), map.Get("near"));
            float far = VisualDistance(map.Get("hub"), map.Get("far"));
            Assert.Greater(far, near * 1.6f);
            Assert.AreEqual(6f, map.Distance("hub", "near"), 0.001f);
            Assert.AreEqual(18f, map.Distance("hub", "far"), 0.001f);
        }

        [Test]
        public void LayoutIsDeterministic()
        {
            const string text = @"
start a
town a Alpha
town b Beta
town c Gamma
track a b 8
track b c 10
track c a 9
";
            var first = RailroadMapParser.Parse(text);
            var second = RailroadMapParser.Parse(text);
            Assert.AreEqual(first.Get("b").X, second.Get("b").X, 0.0001f);
            Assert.AreEqual(first.Get("b").Z, second.Get("b").Z, 0.0001f);
        }

        [Test]
        public void StartTownFacesSouth()
        {
            var map = RailroadMapParser.Parse(@"
start a
town a Alpha
town b Beta
town c Gamma
track a b 8
track b c 8
track c a 8
");
            Assert.Less(map.Get("a").Z, map.Get("b").Z);
            Assert.Less(map.Get("a").Z, map.Get("c").Z);
        }

        [Test]
        public void RejectsUnknownTrackTown()
        {
            var error = Assert.Throws<RailroadMapException>(() => RailroadMapParser.Parse(@"
town a Alpha
track a nowhere
"));
            StringAssert.Contains("nowhere", error.Message);
        }

        [Test]
        public void ParsesLockedTrackWithLengthAndCost()
        {
            var map = RailroadMapParser.Parse(@"
start sc
town sc SanClemente
town hidden Hidden
locked sc hidden 5 100
");
            Assert.AreEqual(1, map.LockedTracks.Count);
            Assert.AreEqual(5f, map.Distance("sc", "hidden"), 0.001f);
            Assert.IsTrue(map.AreLinked("sc", "hidden"));
            Assert.IsTrue(map.IsLocked("sc", "hidden"));
            Assert.AreEqual(100, map.LockedTrack("sc", "hidden").Cost);
            Assert.AreEqual("hidden", map.LockedFrom("sc")[0].Other("sc"));
        }

        [Test]
        public void LockedTrackCanOmitLength()
        {
            var map = RailroadMapParser.Parse(@"
town a Alpha
town b Beta
locked a b 40
");
            Assert.AreEqual(RailroadMapLayout.DefaultTrackLength, map.Distance("a", "b"), 0.001f);
            Assert.AreEqual(40, map.LockedTrack("a", "b").Cost);
        }

        [Test]
        public void LockedTrackCannotReplaceAnOpenTrack()
        {
            var error = Assert.Throws<RailroadMapException>(() => RailroadMapParser.Parse(@"
town a Alpha
town b Beta
track a b 8
locked a b 8 100
"));
            StringAssert.Contains("already has a track", error.Message);
        }

        [Test]
        public void LockedTrackRequiresACost()
        {
            var error = Assert.Throws<RailroadMapException>(() => RailroadMapParser.Parse(@"
town a Alpha
town b Beta
locked a b
"));
            StringAssert.Contains("locked", error.Message);
        }

        [Test]
        public void LockIsAnAliasForLocked()
        {
            var map = RailroadMapParser.Parse(@"
town a Alpha
town b Beta
lock a b 6 25
");
            Assert.IsTrue(map.IsLocked("a", "b"));
            Assert.AreEqual(6f, map.Distance("a", "b"), 0.001f);
            Assert.AreEqual(25, map.LockedTrack("a", "b").Cost);
        }

        [Test]
        public void DefaultMapParsesWithTenTowns()
        {
            var map = RailroadMapParser.Parse(RailroadMaps.TheLocal);
            Assert.AreEqual(10, map.Towns.Count);
            Assert.AreEqual(RailroadGraph.Millhaven, map.StartTownId);
            Assert.IsNotNull(map.FindRoute(RailroadGraph.Willowgate, RailroadGraph.Northspire));
        }

        private static float VisualDistance(TownDef a, TownDef b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
