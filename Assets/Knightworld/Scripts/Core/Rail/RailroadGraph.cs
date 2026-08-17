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

        public static IReadOnlyList<TownDef> Towns { get; } = new[]
        {
            new TownDef(Millhaven, "Millhaven", -5f, -3.5f, Lakeside, Portmere),
            new TownDef(Lakeside, "Lakeside", 6.5f, -2.5f, Millhaven, Hillcrest, Emberford),
            new TownDef(Hillcrest, "Hillcrest", 8f, 6.5f, Lakeside, Emberford),
            new TownDef(Emberford, "Emberford", 0.5f, 8.2f, Hillcrest, Lakeside, Portmere),
            new TownDef(Portmere, "Portmere", -7.2f, 4.2f, Millhaven, Emberford)
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

        public static List<string> FindRoute(string fromId, string toId)
        {
            if (fromId == toId)
                return new List<string> { fromId };

            var queue = new Queue<string>();
            var cameFrom = new Dictionary<string, string>();
            queue.Enqueue(fromId);
            cameFrom[fromId] = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var town = Get(current);
                for (int i = 0; i < town.Links.Count; i++)
                {
                    string next = town.Links[i];
                    if (cameFrom.ContainsKey(next))
                        continue;
                    cameFrom[next] = current;
                    if (next == toId)
                        return Reconstruct(cameFrom, toId);
                    queue.Enqueue(next);
                }
            }

            return null;
        }

        private static List<string> Reconstruct(Dictionary<string, string> cameFrom, string toId)
        {
            var route = new List<string>();
            string cursor = toId;
            while (cursor != null)
            {
                route.Add(cursor);
                cursor = cameFrom[cursor];
            }

            route.Reverse();
            return route;
        }
    }
}
