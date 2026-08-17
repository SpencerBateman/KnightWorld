using System.Collections.Generic;

namespace Knightworld.Core
{
    public sealed class OverworldNode
    {
        public string Id { get; }
        public string Title { get; }
        public string LevelId { get; }
        public float X { get; }
        public float Z { get; }
        public bool StartUnlocked { get; }
        public IReadOnlyList<string> Links { get; }

        public OverworldNode(string id, string title, string levelId, float x, float z, bool startUnlocked, params string[] links)
        {
            Id = id;
            Title = title;
            LevelId = levelId;
            X = x;
            Z = z;
            StartUnlocked = startUnlocked;
            Links = links;
        }
    }

    public static class OverworldGraph
    {
        public const string Meadow = "meadow";
        public const string Lakeshore = "lakeshore";
        public const string Ruins = "ruins";

        public static IReadOnlyList<OverworldNode> Nodes { get; } = new[]
        {
            new OverworldNode(Meadow, "Meadow Crossing", LevelCatalog.Meadow, 0f, 0f, true, Lakeshore, Ruins),
            new OverworldNode(Lakeshore, "Lakeshore Ambush", LevelCatalog.Lakeshore, 7.5f, 2.5f, false, Meadow),
            new OverworldNode(Ruins, "Ruined Keep", LevelCatalog.Ruins, 1.5f, 6.5f, false, Meadow)
        };

        public static OverworldNode Get(string id)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Id == id)
                    return Nodes[i];
            }

            return Nodes[0];
        }

        public static bool AreLinked(string a, string b)
        {
            var node = Get(a);
            for (int i = 0; i < node.Links.Count; i++)
            {
                if (node.Links[i] == b)
                    return true;
            }

            return false;
        }

        public static List<string> FindRoute(string fromId, string toId, HashSet<string> completed)
        {
            if (fromId == toId)
                return new List<string> { fromId };
            if (!CampaignState.IsUnlocked(toId, completed))
                return null;

            var queue = new Queue<string>();
            var cameFrom = new Dictionary<string, string>();
            queue.Enqueue(fromId);
            cameFrom[fromId] = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var node = Get(current);
                for (int i = 0; i < node.Links.Count; i++)
                {
                    string next = node.Links[i];
                    if (cameFrom.ContainsKey(next))
                        continue;
                    bool destination = next == toId;
                    if (!destination && !CanWalkThrough(next, fromId, completed))
                        continue;
                    if (destination && !CampaignState.IsUnlocked(next, completed))
                        continue;
                    cameFrom[next] = current;
                    if (destination)
                        return Reconstruct(cameFrom, toId);
                    queue.Enqueue(next);
                }
            }

            return null;
        }

        private static bool CanWalkThrough(string nodeId, string fromId, HashSet<string> completed)
        {
            if (nodeId == fromId)
                return true;
            var node = Get(nodeId);
            return node.StartUnlocked || completed.Contains(nodeId);
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

    public static class CampaignState
    {
        public static string CurrentNodeId { get; set; } = OverworldGraph.Meadow;
        public static string PendingLevelId { get; set; }
        public static HashSet<string> Completed { get; } = new HashSet<string>();

        public static void Reset()
        {
            CurrentNodeId = OverworldGraph.Meadow;
            PendingLevelId = null;
            Completed.Clear();
        }

        public static bool IsUnlocked(string nodeId) => IsUnlocked(nodeId, Completed);

        public static bool IsUnlocked(string nodeId, HashSet<string> completed)
        {
            var node = OverworldGraph.Get(nodeId);
            if (node.StartUnlocked || completed.Contains(nodeId))
                return true;
            for (int i = 0; i < node.Links.Count; i++)
            {
                if (completed.Contains(node.Links[i]))
                    return true;
            }

            return false;
        }

        public static void RecordVictory()
        {
            if (string.IsNullOrEmpty(PendingLevelId))
                return;
            Completed.Add(PendingLevelId);
            CurrentNodeId = PendingLevelId;
        }

        public static List<string> RouteTo(string nodeId) =>
            OverworldGraph.FindRoute(CurrentNodeId, nodeId, Completed);
    }
}
