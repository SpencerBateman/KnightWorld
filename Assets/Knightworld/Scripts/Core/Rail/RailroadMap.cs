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

    public sealed class LockedTrackDef
    {
        public string A { get; }
        public string B { get; }
        public float Length { get; }
        public int Cost { get; }

        public LockedTrackDef(string a, string b, float length, int cost)
        {
            A = a;
            B = b;
            Length = length;
            Cost = cost;
        }

        public bool Touches(string townId) => townId == A || townId == B;

        public string Other(string townId)
        {
            if (townId == A)
                return B;
            if (townId == B)
                return A;
            return null;
        }
    }

    public sealed class RailroadMap
    {
        private readonly Dictionary<string, TownDef> _byId = new Dictionary<string, TownDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _trackLength = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, LockedTrackDef> _locked = new Dictionary<string, LockedTrackDef>(StringComparer.Ordinal);

        public const float MaxHopSeconds = 120f;

        public string Title { get; }
        public string StartTownId { get; }
        public float SecondsPerDistance { get; }
        public float MinHopSeconds { get; }
        public IReadOnlyList<TownDef> Towns { get; }
        public IReadOnlyList<LandmarkDef> Landmarks { get; }
        public IReadOnlyList<LockedTrackDef> LockedTracks { get; }

        public RailroadMap(
            string title,
            string startTownId,
            float secondsPerDistance,
            float minHopSeconds,
            IReadOnlyList<TownDef> towns,
            IReadOnlyList<LandmarkDef> landmarks,
            IReadOnlyDictionary<string, float> trackLengths,
            IReadOnlyList<LockedTrackDef> lockedTracks = null)
        {
            Title = string.IsNullOrEmpty(title) ? "Railroad" : title;
            Towns = towns;
            Landmarks = landmarks ?? Array.Empty<LandmarkDef>();
            LockedTracks = lockedTracks ?? Array.Empty<LockedTrackDef>();
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

            for (int i = 0; i < LockedTracks.Count; i++)
            {
                var locked = LockedTracks[i];
                _locked[TrackKey(locked.A, locked.B)] = locked;
            }
        }

        public TownDef Get(string id)
        {
            if (id != null && _byId.TryGetValue(id, out var town))
                return town;
            return Towns[0];
        }

        public bool AreLinked(string fromId, string toId)
        {
            if (fromId == null || toId == null || fromId == toId)
                return false;
            if (!_byId.TryGetValue(fromId, out var town))
                return false;
            for (int i = 0; i < town.Links.Count; i++)
            {
                if (town.Links[i] == toId)
                    return true;
            }

            return false;
        }

        public bool IsLocked(string fromId, string toId)
        {
            return fromId != null && toId != null && _locked.ContainsKey(TrackKey(fromId, toId));
        }

        public LockedTrackDef LockedTrack(string fromId, string toId)
        {
            if (fromId == null || toId == null)
                return null;
            _locked.TryGetValue(TrackKey(fromId, toId), out var locked);
            return locked;
        }

        public List<LockedTrackDef> LockedFrom(string townId)
        {
            var found = new List<LockedTrackDef>();
            for (int i = 0; i < LockedTracks.Count; i++)
            {
                if (LockedTracks[i].Touches(townId))
                    found.Add(LockedTracks[i]);
            }

            return found;
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
            if (seconds < MinHopSeconds)
                seconds = MinHopSeconds;
            if (seconds > MaxHopSeconds)
                seconds = MaxHopSeconds;
            return seconds;
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
            return FindRoute(fromId, toId, null);
        }

        public List<string> FindRoute(string fromId, string toId, Func<string, string, bool> canUse)
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
                    if (canUse != null && !canUse(current, next))
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

        public static bool SplitTrackKey(string key, out string a, out string b)
        {
            a = "";
            b = "";
            if (string.IsNullOrEmpty(key))
                return false;
            int split = key.IndexOf('|');
            if (split <= 0 || split >= key.Length - 1)
                return false;
            a = key.Substring(0, split);
            b = key.Substring(split + 1);
            return true;
        }
    }
}
