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
            Links = links ?? Array.Empty<string>();
        }
    }

    public sealed class LandmarkDef
    {
        public const string Lake = "lake";
        public const string Marsh = "marsh";

        public string Kind { get; }
        public string TownId { get; }

        public LandmarkDef(string kind, string townId)
        {
            Kind = kind;
            TownId = townId;
        }
    }

    public sealed class RailroadMap
    {
        private readonly Dictionary<string, TownDef> _byId = new Dictionary<string, TownDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _trackLength = new Dictionary<string, float>(StringComparer.Ordinal);

        public string Title { get; }
        public string StartTownId { get; }
        public float SecondsPerDistance { get; }
        public float MinHopSeconds { get; }
        public IReadOnlyList<TownDef> Towns { get; }
        public IReadOnlyList<LandmarkDef> Landmarks { get; }

        public RailroadMap(
            string title,
            string startTownId,
            float secondsPerDistance,
            float minHopSeconds,
            IReadOnlyList<TownDef> towns,
            IReadOnlyList<LandmarkDef> landmarks,
            IReadOnlyDictionary<string, float> trackLengths)
        {
            Title = string.IsNullOrEmpty(title) ? "Railroad" : title;
            Towns = towns;
            Landmarks = landmarks ?? Array.Empty<LandmarkDef>();
            SecondsPerDistance = secondsPerDistance;
            MinHopSeconds = minHopSeconds;
            for (int i = 0; i < towns.Count; i++)
                _byId[towns[i].Id] = towns[i];
            if (string.IsNullOrEmpty(startTownId) || !_byId.ContainsKey(startTownId))
                StartTownId = towns[0].Id;
            else
                StartTownId = startTownId;
            if (trackLengths != null)
            {
                foreach (var pair in trackLengths)
                    _trackLength[pair.Key] = pair.Value;
            }
        }

        public TownDef Get(string id)
        {
            if (id != null && _byId.TryGetValue(id, out var town))
                return town;
            return Towns[0];
        }

        public float Euclidean(string fromId, string toId)
        {
            var from = Get(fromId);
            var to = Get(toId);
            float dx = from.X - to.X;
            float dz = from.Z - to.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public float Distance(string fromId, string toId)
        {
            if (fromId == toId)
                return 0f;
            if (_trackLength.TryGetValue(TrackKey(fromId, toId), out float length))
                return length;
            return Euclidean(fromId, toId);
        }

        public float RouteDistance(IReadOnlyList<string> route)
        {
            if (route == null || route.Count < 2)
                return 0f;
            float sum = 0f;
            for (int i = 1; i < route.Count; i++)
                sum += Distance(route[i - 1], route[i]);
            return sum;
        }

        public float TravelSeconds(float distance)
        {
            float seconds = distance * SecondsPerDistance;
            return seconds < MinHopSeconds ? MinHopSeconds : seconds;
        }

        public float RouteTravelSeconds(IReadOnlyList<string> route)
        {
            if (route == null || route.Count < 2)
                return 0f;
            float seconds = 0f;
            for (int i = 1; i < route.Count; i++)
                seconds += TravelSeconds(Distance(route[i - 1], route[i]));
            return seconds;
        }

        public List<string> FindRoute(string fromId, string toId)
        {
            if (fromId == toId)
                return new List<string> { fromId };

            var dist = new Dictionary<string, float>(StringComparer.Ordinal);
            var prev = new Dictionary<string, string>(StringComparer.Ordinal);
            var remaining = new List<string>();
            for (int i = 0; i < Towns.Count; i++)
            {
                string id = Towns[i].Id;
                dist[id] = float.MaxValue;
                remaining.Add(id);
            }

            if (!dist.ContainsKey(fromId) || !dist.ContainsKey(toId))
                return null;

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
                    if (!dist.ContainsKey(next))
                        continue;
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

        public static string TrackKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
        }
    }
}
