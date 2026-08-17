using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public static class RailroadMapLayout
    {
        public const float DefaultTrackLength = 8f;

        public static void Place(IReadOnlyList<string> ids, IReadOnlyDictionary<string, float> lengths, string startId, float[] x, float[] z)
        {
            int n = ids.Count;
            if (n == 0)
                return;
            if (n == 1)
            {
                x[0] = 0f;
                z[0] = 0f;
                return;
            }

            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < n; i++)
                index[ids[i]] = i;

            if (n == 2)
            {
                float len = EdgeLength(ids[0], ids[1], lengths);
                int start = startId != null && index.TryGetValue(startId, out int s) ? s : 0;
                int other = start == 0 ? 1 : 0;
                x[start] = 0f;
                z[start] = -len * 0.5f;
                x[other] = 0f;
                z[other] = len * 0.5f;
                return;
            }

            float[][] dist = Pairwise(ids, index, lengths);
            SeedCircle(ids, dist, x, z);
            Relax(dist, x, z);
            MatchEdgeScale(ids, index, lengths, x, z);
            Recenter(x, z);
            Orient(ids, index, startId, x, z);
        }

        private static float[][] Pairwise(IReadOnlyList<string> ids, Dictionary<string, int> index, IReadOnlyDictionary<string, float> lengths)
        {
            int n = ids.Count;
            const float inf = 1e8f;
            var d = new float[n][];
            for (int i = 0; i < n; i++)
            {
                d[i] = new float[n];
                for (int j = 0; j < n; j++)
                    d[i][j] = i == j ? 0f : inf;
            }

            if (lengths != null)
            {
                foreach (var pair in lengths)
                {
                    SplitKey(pair.Key, out string a, out string b);
                    if (!index.TryGetValue(a, out int ia) || !index.TryGetValue(b, out int ib))
                        continue;
                    float len = pair.Value;
                    if (len < d[ia][ib])
                    {
                        d[ia][ib] = len;
                        d[ib][ia] = len;
                    }
                }
            }

            for (int k = 0; k < n; k++)
            {
                for (int i = 0; i < n; i++)
                {
                    float dik = d[i][k];
                    if (dik >= inf)
                        continue;
                    for (int j = 0; j < n; j++)
                    {
                        float alt = dik + d[k][j];
                        if (alt < d[i][j])
                            d[i][j] = alt;
                    }
                }
            }

            float max = 0f;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (d[i][j] < inf && d[i][j] > max)
                        max = d[i][j];
                }
            }

            if (max <= 0f)
                max = DefaultTrackLength;
            float gap = max * 1.8f;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (d[i][j] >= inf)
                        d[i][j] = gap;
                }
            }

            return d;
        }

        private static void SeedCircle(IReadOnlyList<string> ids, float[][] dist, float[] x, float[] z)
        {
            int n = ids.Count;
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    sum += dist[i][j];
                    count++;
                }
            }

            float avg = count > 0 ? sum / count : DefaultTrackLength;
            float radius = Math.Max(avg * 0.55f, DefaultTrackLength);
            for (int i = 0; i < n; i++)
            {
                double angle = Math.PI * 2.0 * i / n - Math.PI * 0.5;
                x[i] = (float)(Math.Cos(angle) * radius);
                z[i] = (float)(Math.Sin(angle) * radius);
            }
        }

        private static void Relax(float[][] dist, float[] x, float[] z)
        {
            int n = x.Length;
            int iterations = 70 + n * 40;
            if (iterations > 700)
                iterations = 700;
            for (int step = 0; step < iterations; step++)
            {
                float rate = 0.28f * (1f - step / (float)iterations);
                if (rate < 0.018f)
                    rate = 0.018f;
                for (int i = 0; i < n; i++)
                {
                    float fx = 0f;
                    float fz = 0f;
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j)
                            continue;
                        float dx = x[i] - x[j];
                        float dz = z[i] - z[j];
                        float gap = (float)Math.Sqrt(dx * dx + dz * dz);
                        if (gap < 0.0001f)
                        {
                            dx = 0.02f * (1 + i);
                            dz = 0.02f * (1 + j);
                            gap = (float)Math.Sqrt(dx * dx + dz * dz);
                        }

                        float ideal = dist[i][j];
                        if (ideal < 0.0001f)
                            continue;
                        float pull = (gap - ideal) / (ideal * ideal);
                        fx += (dx / gap) * pull;
                        fz += (dz / gap) * pull;
                    }

                    x[i] -= fx * rate;
                    z[i] -= fz * rate;
                }
            }
        }

        private static void MatchEdgeScale(
            IReadOnlyList<string> ids,
            Dictionary<string, int> index,
            IReadOnlyDictionary<string, float> lengths,
            float[] x,
            float[] z)
        {
            if (lengths == null || lengths.Count == 0)
                return;
            float visSum = 0f;
            float lenSum = 0f;
            foreach (var pair in lengths)
            {
                SplitKey(pair.Key, out string a, out string b);
                if (!index.TryGetValue(a, out int ia) || !index.TryGetValue(b, out int ib))
                    continue;
                float dx = x[ia] - x[ib];
                float dz = z[ia] - z[ib];
                float vis = (float)Math.Sqrt(dx * dx + dz * dz);
                if (vis < 0.0001f)
                    continue;
                visSum += vis;
                lenSum += pair.Value;
            }

            if (visSum < 0.0001f || lenSum <= 0f)
                return;
            float scale = lenSum / visSum;
            for (int i = 0; i < x.Length; i++)
            {
                x[i] *= scale;
                z[i] *= scale;
            }
        }

        private static void Recenter(float[] x, float[] z)
        {
            float sx = 0f;
            float sz = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                sx += x[i];
                sz += z[i];
            }

            float n = x.Length;
            sx /= n;
            sz /= n;
            for (int i = 0; i < x.Length; i++)
            {
                x[i] -= sx;
                z[i] -= sz;
            }
        }

        private static void Orient(IReadOnlyList<string> ids, Dictionary<string, int> index, string startId, float[] x, float[] z)
        {
            int start = 0;
            if (startId != null && index.TryGetValue(startId, out int found))
                start = found;
            float sx = x[start];
            float sz = z[start];
            double current = Math.Atan2(sx, sz);
            double target = Math.Atan2(0.0, -1.0);
            double rot = target - current;
            double cos = Math.Cos(rot);
            double sin = Math.Sin(rot);
            for (int i = 0; i < x.Length; i++)
            {
                double nx = x[i] * cos - z[i] * sin;
                double nz = x[i] * sin + z[i] * cos;
                x[i] = (float)nx;
                z[i] = (float)nz;
            }

            string pivotId = null;
            for (int i = 0; i < ids.Count; i++)
            {
                if (i == start)
                    continue;
                if (pivotId == null || string.CompareOrdinal(ids[i], pivotId) < 0)
                    pivotId = ids[i];
            }

            if (pivotId != null && x[index[pivotId]] < 0f)
            {
                for (int i = 0; i < x.Length; i++)
                    x[i] = -x[i];
            }
        }

        private static float EdgeLength(string a, string b, IReadOnlyDictionary<string, float> lengths)
        {
            if (lengths != null && lengths.TryGetValue(RailroadMap.TrackKey(a, b), out float len))
                return len;
            return DefaultTrackLength;
        }

        private static void SplitKey(string key, out string a, out string b)
        {
            int bar = key.IndexOf('|');
            if (bar <= 0)
            {
                a = key;
                b = key;
                return;
            }

            a = key.Substring(0, bar);
            b = key.Substring(bar + 1);
        }
    }
}
