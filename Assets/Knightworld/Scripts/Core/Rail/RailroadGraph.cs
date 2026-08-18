using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public static class RailroadGraph
    {
        public const string Millhaven = "millhaven";
        public const string Lakeside = "lakeside";
        public const string Hillcrest = "hillcrest";
        public const string Emberford = "emberford";
        public const string Portmere = "portmere";
        public const string Willowgate = "willowgate";
        public const string Saltmarsh = "saltmarsh";
        public const string Copsewood = "copsewood";
        public const string Northspire = "northspire";
        public const string Stonebridge = "stonebridge";

        static RailroadGraph()
        {
            UseDefault();
        }

        public static RailroadMap Map { get; private set; }

        public static IReadOnlyList<TownDef> Towns => Map.Towns;
        public static string StartTownId => Map.StartTownId;
        public static float SecondsPerDistance => Map.SecondsPerDistance;
        public static float MinHopSeconds => Map.MinHopSeconds;

        public static void UseDefault()
        {
            Use(RailroadMapParser.Parse(RailroadMaps.TheLocal));
        }

        public static void Use(RailroadMap map)
        {
            if (map == null || map.Towns == null || map.Towns.Count == 0)
                throw new ArgumentException("Map needs at least one town.");
            Map = map;
        }

        public static TownDef Get(string id) => Map.Get(id);

        public static bool AreLinked(string fromId, string toId) => Map.AreLinked(fromId, toId);

        public static bool IsLocked(string fromId, string toId) => Map.IsLocked(fromId, toId);

        public static LockedTrackDef LockedTrack(string fromId, string toId) => Map.LockedTrack(fromId, toId);

        public static List<LockedTrackDef> LockedFrom(string townId) => Map.LockedFrom(townId);

        public static float Distance(string fromId, string toId) => Map.Distance(fromId, toId);

        public static float RouteDistance(IReadOnlyList<string> route) => Map.RouteDistance(route);

        public static float TravelSeconds(float distance) => Map.TravelSeconds(distance);

        public static float RouteTravelSeconds(IReadOnlyList<string> route) => Map.RouteTravelSeconds(route);

        public static List<string> FindRoute(string fromId, string toId) => Map.FindRoute(fromId, toId);
    }
}
