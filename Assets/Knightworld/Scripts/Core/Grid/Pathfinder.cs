using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public static class Pathfinder
    {
        private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        public static List<GridPos> FindPath(
            GridMap map,
            GridPos start,
            GridPos goal,
            Func<GridPos, bool> blocksPass,
            Func<GridPos, bool> blocksStand)
        {
            if (start == goal)
                return new List<GridPos> { start };
            if (!map.IsWalkable(goal) || blocksStand(goal))
                return null;

            var cameFrom = new Dictionary<GridPos, GridPos>();
            var gScore = new Dictionary<GridPos, int> { [start] = 0 };
            var open = new List<GridPos> { start };
            var openSet = new HashSet<GridPos> { start };

            while (open.Count > 0)
            {
                int bestIndex = 0;
                int bestF = int.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    var pos = open[i];
                    int f = gScore[pos] + pos.Chebyshev(goal);
                    if (f < bestF)
                    {
                        bestF = f;
                        bestIndex = i;
                    }
                }

                var current = open[bestIndex];
                if (current == goal)
                    return Reconstruct(cameFrom, current);

                open.RemoveAt(bestIndex);
                openSet.Remove(current);

                foreach (var next in Neighbors(map, current))
                {
                    bool isGoal = next == goal;
                    if (isGoal)
                    {
                        if (blocksStand(next))
                            continue;
                    }
                    else if (blocksPass(next))
                    {
                        continue;
                    }

                    int tentative = gScore[current] + 1;
                    if (gScore.TryGetValue(next, out int existing) && tentative >= existing)
                        continue;

                    cameFrom[next] = current;
                    gScore[next] = tentative;
                    if (openSet.Add(next))
                        open.Add(next);
                }
            }

            return null;
        }

        public static HashSet<GridPos> FindReachable(
            GridMap map,
            GridPos start,
            int maxSquares,
            Func<GridPos, bool> blocksPass,
            Func<GridPos, bool> blocksStand)
        {
            var reachable = new HashSet<GridPos>();
            var cost = new Dictionary<GridPos, int> { [start] = 0 };
            var open = new Queue<GridPos>();
            open.Enqueue(start);

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                int currentCost = cost[current];
                if (currentCost >= maxSquares)
                    continue;

                foreach (var next in Neighbors(map, current))
                {
                    if (blocksPass(next))
                        continue;
                    int nextCost = currentCost + 1;
                    if (nextCost > maxSquares)
                        continue;
                    if (cost.TryGetValue(next, out int existing) && existing <= nextCost)
                        continue;
                    cost[next] = nextCost;
                    if (!blocksStand(next))
                        reachable.Add(next);
                    open.Enqueue(next);
                }
            }

            return reachable;
        }

        private static List<GridPos> Reconstruct(Dictionary<GridPos, GridPos> cameFrom, GridPos current)
        {
            var path = new List<GridPos> { current };
            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static IEnumerable<GridPos> Neighbors(GridMap map, GridPos current)
        {
            for (int i = 0; i < Dx.Length; i++)
            {
                var next = current.Offset(Dx[i], Dy[i]);
                if (map.CanStep(current, next))
                    yield return next;
            }
        }
    }
}
