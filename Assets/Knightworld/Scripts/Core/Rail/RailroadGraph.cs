using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public sealed class TownDef
    {
        public string Id { get; }
        public string Name { get; }
        public float X { get; }
        public float Z { get; }
        public IReadOnlyList<string> Links { get; }

        public TownDef(string id, string name, float x, float z, params string[] links)
        {
            Id = id;
            Name = name;
            X = x;
            Z = z;
            Links = links;
        }
    }

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
        public const float SecondsPerDistance = 0.45f;
        public const float MinHopSeconds = 0.7f;

        public static IReadOnlyList<TownDef> Towns { get; } = new[]
        {
            new TownDef(Millhaven, "Millhaven", -5f, -3.5f, Lakeside, Portmere, Willowgate),
            new TownDef(Lakeside, "Lakeside", 6.5f, -2.5f, Millhaven, Hillcrest, Emberford, Saltmarsh, Copsewood),
            new TownDef(Hillcrest, "Hillcrest", 8f, 6.5f, Lakeside, Emberford, Copsewood, Northspire),
            new TownDef(Emberford, "Emberford", 0.5f, 8.2f, Hillcrest, Lakeside, Portmere, Stonebridge, Northspire),
            new TownDef(Portmere, "Portmere", -7.2f, 4.2f, Millhaven, Emberford, Willowgate, Stonebridge),
            new TownDef(Willowgate, "Willowgate", -12.5f, -1.2f, Millhaven, Portmere),
            new TownDef(Saltmarsh, "Saltmarsh", 13.5f, -6.5f, Lakeside, Copsewood),
            new TownDef(Copsewood, "Copsewood", 13.2f, 3.2f, Lakeside, Hillcrest, Saltmarsh),
            new TownDef(Northspire, "Northspire", 4.2f, 15f, Hillcrest, Emberford, Stonebridge),
            new TownDef(Stonebridge, "Stonebridge", -8.5f, 11.8f, Portmere, Emberford, Northspire)
        };

        public static TownDef Get(string id)
        {
            for (int i = 0; i < Towns.Count; i++)
            {
                if (Towns[i].Id == id)
                    return Towns[i];
            }

            return Towns[0];
        }

        public static float Distance(string fromId, string toId)
        {
            var from = Get(fromId);
            var to = Get(toId);
            float dx = from.X - to.X;
            float dz = from.Z - to.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public static float RouteDistance(IReadOnlyList<string> route)
        {
            if (route == null || route.Count < 2)
                return 0f;
            float sum = 0f;
            for (int i = 1; i < route.Count; i++)
                sum += Distance(route[i - 1], route[i]);
            return sum;
        }

        public static float TravelSeconds(float distance)
        {
            float seconds = distance * SecondsPerDistance;
            return seconds < MinHopSeconds ? MinHopSeconds : seconds;
        }

        public static float RouteTravelSeconds(IReadOnlyList<string> route)
        {
            if (route == null || route.Count < 2)
                return 0f;
            float seconds = 0f;
            for (int i = 1; i < route.Count; i++)
            {
                float hop = TravelSeconds(Distance(route[i - 1], route[i]));
                seconds += hop;
            }

            return seconds;
        }

        public static List<string> FindRoute(string fromId, string toId)
        {
            if (fromId == toId)
                return new List<string> { fromId };

            var dist = new Dictionary<string, float>();
            var prev = new Dictionary<string, string>();
            var remaining = new List<string>();
            for (int i = 0; i < Towns.Count; i++)
            {
                string id = Towns[i].Id;
                dist[id] = float.MaxValue;
                remaining.Add(id);
            }

            dist[fromId] = 0f;
            while (remaining.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < remaining.Count; i++)
                {
                    if (dist[remaining[i]] < dist[remaining[best]])
                        best = i;
                }

                string current = remaining[best];
                remaining.RemoveAt(best);
                if (current == toId)
                    break;
                if (dist[current] >= float.MaxValue)
                    break;

                var town = Get(current);
                for (int i = 0; i < town.Links.Count; i++)
                {
                    string next = town.Links[i];
                    float alt = dist[current] + Distance(current, next);
                    if (alt >= dist[next])
                        continue;
                    dist[next] = alt;
                    prev[next] = current;
                }
            }

            if (!prev.ContainsKey(toId))
                return null;

            var route = new List<string>();
            string cursor = toId;
            while (cursor != fromId)
            {
                route.Add(cursor);
                cursor = prev[cursor];
            }

            route.Add(fromId);
            route.Reverse();
            return route;
        }
    }
}
