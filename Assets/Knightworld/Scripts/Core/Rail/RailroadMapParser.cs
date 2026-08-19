using System;
using System.Collections.Generic;
using System.Globalization;

namespace Knightworld.Core
{
    public sealed class RailroadMapException : Exception
    {
        public RailroadMapException(string message) : base(message)
        {
        }
    }

    public static class RailroadMapParser
    {
        public static RailroadMap Parse(string text)
        {
            if (text == null)
                throw new RailroadMapException("Map text is missing.");

            string title = "";
            string start = "";
            float speed = 6f;
            float minHop = 20f;
            var towns = new List<TownDraft>();
            var tracks = new List<TrackDraft>();
            var locked = new List<LockedDraft>();
            var landmarks = new List<LandmarkDef>();
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                var tokens = Tokenize(lines[i]);
                if (tokens.Count == 0)
                    continue;
                string command = tokens[0].ToLowerInvariant();
                try
                {
                    switch (command)
                    {
                        case "title":
                            title = Rest(lines[i], tokens[0]).Trim();
                            break;
                        case "start":
                            Need(tokens, 2, "start <townId>");
                            start = NormalizeId(tokens[1]);
                            break;
                        case "speed":
                            Need(tokens, 2, "speed <secondsPerDistance>");
                            speed = ParsePositive(tokens[1], "speed");
                            break;
                        case "minhop":
                            Need(tokens, 2, "minhop <seconds>");
                            minHop = ParsePositive(tokens[1], "minhop");
                            break;
                        case "town":
                            towns.Add(ParseTown(tokens));
                            break;
                        case "track":
                            tracks.Add(ParseTrack(tokens));
                            break;
                        case "locked":
                        case "lock":
                            locked.Add(ParseLocked(tokens));
                            break;
                        case "landmark":
                            Need(tokens, 3, "landmark <lake|marsh> <townId>");
                            landmarks.Add(ParseLandmark(tokens));
                            break;
                        default:
                            throw new RailroadMapException("Unknown command '" + tokens[0] + "'. Use title, start, speed, minhop, town, track, locked, or landmark.");
                    }
                }
                catch (RailroadMapException ex)
                {
                    throw new RailroadMapException("Line " + lineNo + ": " + ex.Message);
                }
            }

            if (towns.Count == 0)
                throw new RailroadMapException("Map needs at least one town.");

            var byId = new Dictionary<string, TownDraft>(StringComparer.Ordinal);
            for (int i = 0; i < towns.Count; i++)
            {
                var town = towns[i];
                if (byId.ContainsKey(town.Id))
                    throw new RailroadMapException("Duplicate town id '" + town.Id + "'.");
                byId[town.Id] = town;
            }

            if (!string.IsNullOrEmpty(start) && !byId.ContainsKey(start))
                throw new RailroadMapException("start town '" + start + "' is not defined.");

            var lengths = new Dictionary<string, float>(StringComparer.Ordinal);
            for (int i = 0; i < towns.Count; i++)
            {
                var town = towns[i];
                for (int n = 0; n < town.Neighbors.Count; n++)
                    AddTrack(byId, lengths, town.Id, town.Neighbors[n], null);
            }

            for (int i = 0; i < tracks.Count; i++)
                AddTrack(byId, lengths, tracks[i].A, tracks[i].B, tracks[i].Length);

            var lockedTracks = new List<LockedTrackDef>();
            for (int i = 0; i < locked.Count; i++)
            {
                var draft = locked[i];
                string key = RailroadMap.TrackKey(draft.A, draft.B);
                if (lengths.ContainsKey(key))
                    throw new RailroadMapException("locked " + draft.A + " " + draft.B + " already has a track.");
                AddTrack(byId, lengths, draft.A, draft.B, draft.Length);
                lockedTracks.Add(new LockedTrackDef(draft.A, draft.B, lengths[key], draft.Cost));
            }

            var ids = new string[towns.Count];
            for (int i = 0; i < towns.Count; i++)
                ids[i] = towns[i].Id;
            var xs = new float[towns.Count];
            var zs = new float[towns.Count];
            string layoutStart = !string.IsNullOrEmpty(start) ? start : towns[0].Id;
            RailroadMapLayout.Place(ids, lengths, layoutStart, xs, zs);
            for (int i = 0; i < towns.Count; i++)
            {
                towns[i].X = xs[i];
                towns[i].Z = zs[i];
            }

            var defs = new TownDef[towns.Count];
            for (int i = 0; i < towns.Count; i++)
            {
                var draft = towns[i];
                var links = new string[draft.Links.Count];
                draft.Links.CopyTo(links);
                Array.Sort(links, StringComparer.Ordinal);
                defs[i] = new TownDef(draft.Id, draft.Name, draft.X, draft.Z, links);
            }

            var keptLandmarks = new List<LandmarkDef>();
            for (int i = 0; i < landmarks.Count; i++)
            {
                if (!byId.ContainsKey(landmarks[i].TownId))
                    throw new RailroadMapException("landmark town '" + landmarks[i].TownId + "' is not defined.");
                keptLandmarks.Add(landmarks[i]);
            }

            return new RailroadMap(title, start, speed, minHop, defs, keptLandmarks, lengths, lockedTracks);
        }

        private static void AddTrack(
            Dictionary<string, TownDraft> byId,
            Dictionary<string, float> lengths,
            string fromId,
            string toId,
            float? length)
        {
            if (!byId.ContainsKey(fromId))
                throw new RailroadMapException("Unknown town '" + fromId + "'.");
            if (!byId.ContainsKey(toId))
                throw new RailroadMapException("Unknown town '" + toId + "'.");
            if (fromId == toId)
                throw new RailroadMapException("Track cannot connect a town to itself (" + fromId + ").");

            var from = byId[fromId];
            var to = byId[toId];
            if (!from.Links.Contains(toId))
                from.Links.Add(toId);
            if (!to.Links.Contains(fromId))
                to.Links.Add(fromId);

            string key = RailroadMap.TrackKey(fromId, toId);
            float resolved = length ?? RailroadMapLayout.DefaultTrackLength;
            if (resolved <= 0f)
                throw new RailroadMapException("Track " + fromId + " " + toId + " needs a distance greater than 0.");
            lengths[key] = resolved;
        }

        private static TownDraft ParseTown(List<string> tokens)
        {
            Need(tokens, 3, "town <id> <name> [neighbors...]");
            var town = new TownDraft
            {
                Id = NormalizeId(tokens[1]),
                Name = tokens[2]
            };
            if (string.IsNullOrEmpty(town.Name))
                throw new RailroadMapException("Town name is empty.");
            for (int i = 3; i < tokens.Count; i++)
                town.Neighbors.Add(NormalizeId(tokens[i]));
            return town;
        }

        private static TrackDraft ParseTrack(List<string> tokens)
        {
            Need(tokens, 3, "track <id> <id> [length]");
            var track = new TrackDraft
            {
                A = NormalizeId(tokens[1]),
                B = NormalizeId(tokens[2])
            };
            if (tokens.Count >= 4)
                track.Length = ParsePositive(tokens[3], "length");
            else
                track.Length = null;
            if (tokens.Count > 4)
                throw new RailroadMapException("Too many values. Use track <id> <id> [length].");
            return track;
        }

        private static LockedDraft ParseLocked(List<string> tokens)
        {
            Need(tokens, 4, "locked <id> <id> [length] <cost>");
            if (tokens.Count > 5)
                throw new RailroadMapException("Too many values. Use locked <id> <id> [length] <cost>.");
            var locked = new LockedDraft
            {
                A = NormalizeId(tokens[1]),
                B = NormalizeId(tokens[2])
            };
            if (tokens.Count == 4)
            {
                locked.Length = null;
                locked.Cost = ParseCost(tokens[3]);
            }
            else
            {
                locked.Length = ParsePositive(tokens[3], "length");
                locked.Cost = ParseCost(tokens[4]);
            }

            return locked;
        }

        private static LandmarkDef ParseLandmark(List<string> tokens)
        {
            string kind = tokens[1].ToLowerInvariant();
            if (kind != LandmarkDef.Lake && kind != LandmarkDef.Marsh)
                throw new RailroadMapException("Landmark kind must be lake or marsh.");
            return new LandmarkDef(kind, NormalizeId(tokens[2]));
        }

        private static void Need(List<string> tokens, int count, string usage)
        {
            if (tokens.Count < count)
                throw new RailroadMapException("Expected " + usage + ".");
        }

        private static int ParseCost(string text)
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cost) || cost <= 0)
                throw new RailroadMapException("cost must be a whole number greater than 0.");
            return cost;
        }

        private static float ParsePositive(string text, string label)
        {
            float value = ParseFloat(text, label);
            if (value <= 0f)
                throw new RailroadMapException(label + " must be greater than 0.");
            return value;
        }

        private static float ParseFloat(string text, string label)
        {
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                throw new RailroadMapException("Could not read " + label + " number '" + text + "'.");
            return value;
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new RailroadMapException("Town id is empty.");
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                    continue;
                throw new RailroadMapException("Town id '" + id + "' can only use letters, numbers, _ and -.");
            }

            return id.ToLowerInvariant();
        }

        private static string Rest(string line, string command)
        {
            int i = 0;
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;
            i += command.Length;
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;
            int end = line.Length;
            int hash = line.IndexOf('#', i);
            if (hash >= 0)
                end = hash;
            return line.Substring(i, end - i).Trim().Trim('"');
        }

        internal static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(line))
                return tokens;
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];
                if (c == '#')
                    break;
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    char quote = c;
                    i++;
                    int start = i;
                    while (i < line.Length && line[i] != quote)
                        i++;
                    tokens.Add(line.Substring(start, i - start));
                    if (i < line.Length)
                        i++;
                    continue;
                }

                int from = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i]) && line[i] != '#')
                    i++;
                tokens.Add(line.Substring(from, i - from));
            }

            return tokens;
        }

        private sealed class TownDraft
        {
            public string Id;
            public string Name;
            public float X;
            public float Z;
            public readonly List<string> Neighbors = new List<string>();
            public readonly List<string> Links = new List<string>();
        }

        private sealed class TrackDraft
        {
            public string A;
            public string B;
            public float? Length;
        }

        private sealed class LockedDraft
        {
            public string A;
            public string B;
            public float? Length;
            public int Cost;
        }
    }
}
