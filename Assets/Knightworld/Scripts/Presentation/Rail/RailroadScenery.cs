using System;
using System.Collections.Generic;
using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class RailroadScenery
    {
        private readonly Transform _root;
        private readonly RailroadView _view;
        private readonly List<Vector3> _towns = new List<Vector3>();
        private readonly List<Vector3> _exits = new List<Vector3>();
        private readonly List<Track> _tracks = new List<Track>();
        private readonly List<TownVibe> _vibes = new List<TownVibe>();
        private readonly List<Vector3> _waters = new List<Vector3>();
        private Vector3 _center;
        private Vector3 _min;
        private Vector3 _max;
        private float _radius;
        private int _rng;

        public RailroadScenery(Transform root, RailroadView view)
        {
            _root = root;
            _view = view;
        }

        public void Build()
        {
            var map = RailroadGraph.Map;
            Collect(map);
            _rng = StableSeed(map);
            PaintSky();
            PaintGround();
            PaintWater(map);
            PaintMountains();
            PaintForests();
            PaintRocks();
            PaintClouds();
        }

        public void ApplyCamera(Camera camera)
        {
            if (camera == null)
                return;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.78f, 0.92f);
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, _radius * 8f + 80f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.90f);
            RenderSettings.fogStartDistance = _radius * 1.15f + 8f;
            RenderSettings.fogEndDistance = _radius * 3.4f + 36f;
            RenderSettings.ambientLight = new Color(0.78f, 0.82f, 0.88f);
        }

        private void Collect(RailroadMap map)
        {
            _min = new Vector3(float.MaxValue, 0f, float.MaxValue);
            _max = new Vector3(float.MinValue, 0f, float.MinValue);
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            for (int i = 0; i < map.Towns.Count; i++)
            {
                var town = map.Towns[i];
                if (town.Z < minZ)
                    minZ = town.Z;
                if (town.Z > maxZ)
                    maxZ = town.Z;
            }

            float spanZ = Mathf.Max(0.001f, maxZ - minZ);
            for (int i = 0; i < map.Towns.Count; i++)
            {
                var town = map.Towns[i];
                Vector3 pos = _view.WorldPos(town);
                _towns.Add(pos);
                _vibes.Add(TownVibe.From(town, map.Landmarks, (town.Z - minZ) / spanZ));
                _min = Vector3.Min(_min, pos);
                _max = Vector3.Max(_max, pos);
                Vector3 exit = Vector3.zero;
                int links = town.Links.Count;
                for (int n = 0; n < links; n++)
                    exit += _view.WorldPos(RailroadGraph.Get(town.Links[n]));
                if (links > 0)
                    exit = (exit / links - pos).normalized;
                _exits.Add(exit.sqrMagnitude > 0.01f ? exit : Vector3.forward);
            }

            var drawn = new HashSet<string>();
            for (int i = 0; i < map.Towns.Count; i++)
            {
                var town = map.Towns[i];
                for (int n = 0; n < town.Links.Count; n++)
                {
                    string other = town.Links[n];
                    string key = town.Id.CompareTo(other) < 0 ? town.Id + ">" + other : other + ">" + town.Id;
                    if (!drawn.Add(key))
                        continue;
                    _tracks.Add(new Track { A = _view.WorldPos(town), B = _view.WorldPos(RailroadGraph.Get(other)) });
                }
            }

            _center = (_min + _max) * 0.5f;
            _radius = 0f;
            for (int i = 0; i < _towns.Count; i++)
            {
                float d = Vector3.Distance(Flatten(_towns[i]), Flatten(_center));
                if (d > _radius)
                    _radius = d;
            }

            if (_radius < 8f)
                _radius = 8f;
        }

        private void PaintSky()
        {
            var sun = Prim(PrimitiveType.Sphere, "Sun", _center + new Vector3(_radius * 1.6f, _radius * 1.15f + 18f, _radius * 0.55f), new Vector3(6.5f, 6.5f, 6.5f), RailroadMaterials.Sun);
            sun.transform.localScale = new Vector3(7.5f, 7.5f, 7.5f);

            var haze = Prim(PrimitiveType.Cylinder, "Horizon", new Vector3(_center.x, -1.6f, _center.z), new Vector3(_radius * 0.62f + 8f, 0.08f, _radius * 0.62f + 8f), RailroadMaterials.Haze);
            haze.transform.localScale = new Vector3((_radius * 6.2f + 40f), 0.12f, (_radius * 6.2f + 40f));
        }

        private void PaintGround()
        {
            float pad = 22f;
            Vector3 size = _max - _min;
            Prim(PrimitiveType.Plane, "Earth", WorldLayers.Lift(_center, WorldLayers.Earth), new Vector3((size.x + pad + 18f) / 10f, 1f, (size.z + pad + 18f) / 10f), RailroadMaterials.Earth);
            Prim(PrimitiveType.Plane, "Ground", WorldLayers.Lift(_center, WorldLayers.Grass), new Vector3((size.x + pad) / 10f, 1f, (size.z + pad) / 10f), RailroadMaterials.Grass);

            int meadows = 6 + _towns.Count;
            for (int i = 0; i < meadows; i++)
            {
                Vector3 at = SampleMap(0.15f, 0.92f);
                if (Blocked(at, 2.4f, true))
                    continue;
                float w = Rand(4.5f, 9f);
                float d = Rand(3.5f, 7.5f);
                var mat = Rand01() > 0.45f ? RailroadMaterials.Meadow : RailroadMaterials.DeepGrass;
                Prim(PrimitiveType.Cylinder, "Meadow", WorldLayers.Lift(at, WorldLayers.Meadow, i), new Vector3(w, WorldLayers.MeadowHalf, d), mat);
            }
        }

        private void PaintWater(RailroadMap map)
        {
            for (int i = 0; i < _towns.Count; i++)
            {
                var vibe = _vibes[i];
                Vector3 town = _towns[i];
                Vector3 outward = Outward(town);
                Vector3 side = Vector3.Cross(Vector3.up, outward);
                if (side.sqrMagnitude < 0.01f)
                    side = Vector3.right;
                side.Normalize();

                if (vibe.Island >= 0.55f)
                    IslandWater(town, _exits[i], vibe);
                else if (vibe.Water >= 0.4f || vibe.Coast >= 0.45f || vibe.Marsh >= 0.5f)
                    ShoreWater(town, outward, side, vibe);

                if (vibe.Marsh >= 0.4f)
                    Pond(town + (outward + side) * Rand(3.2f, 5.2f), Rand(2.2f, 3.4f), Rand(1.6f, 2.6f), RailroadMaterials.MarshWater, 0.6f);
            }

            for (int i = 0; i < map.Landmarks.Count; i++)
            {
                var mark = map.Landmarks[i];
                Vector3 at = _view.WorldPos(RailroadGraph.Get(mark.TownId));
                Vector3 outward = Outward(at);
                if (mark.Kind == LandmarkDef.Lake)
                    Pond(at + outward * 3.4f + Vector3.Cross(Vector3.up, outward).normalized * 1.4f, 4.6f, 2.8f, RailroadMaterials.DeepWater, 1f);
                else if (mark.Kind == LandmarkDef.Marsh)
                    Pond(at + outward * 3f, 3.6f, 2.4f, RailroadMaterials.MarshWater, 0.7f);
            }

            TryCreek();
        }

        private void IslandWater(Vector3 town, Vector3 landBridge, TownVibe vibe)
        {
            int lobes = 7;
            for (int i = 0; i < lobes; i++)
            {
                float t = i / (float)lobes;
                float ang = t * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                if (Vector3.Dot(dir, -landBridge) > 0.72f)
                    continue;
                Vector3 at = town + dir * Rand(4.4f, 6.4f);
                Pond(at, Rand(3.4f, 5.2f), Rand(2.4f, 3.8f), vibe.Coast > 0.3f ? RailroadMaterials.ShallowWater : RailroadMaterials.DeepWater, 1f);
            }
        }

        private void ShoreWater(Vector3 town, Vector3 outward, Vector3 side, TownVibe vibe)
        {
            var water = vibe.Marsh >= 0.5f ? RailroadMaterials.MarshWater : RailroadMaterials.DeepWater;
            Vector3 a = town + outward * Rand(3.8f, 5.6f);
            Pond(a, Rand(4.2f, 6.4f), Rand(2.6f, 4.2f), water, 1f);
            Pond(a + side * Rand(2.2f, 3.8f), Rand(2.8f, 4.2f), Rand(1.8f, 3f), RailroadMaterials.ShallowWater, 0.55f);
            if (vibe.Coast >= 0.5f)
            {
                Prim(PrimitiveType.Cylinder, "Shore", WorldLayers.Lift(a - outward * 0.8f, WorldLayers.Sand), new Vector3(Rand(3f, 4.6f), WorldLayers.SandHalf, Rand(1.6f, 2.4f)), RailroadMaterials.Sand);
            }
        }

        private void TryCreek()
        {
            int source = -1;
            int sink = -1;
            float bestMount = 0.45f;
            float bestWater = 0.4f;
            for (int i = 0; i < _vibes.Count; i++)
            {
                if (_vibes[i].Mountain > bestMount)
                {
                    bestMount = _vibes[i].Mountain;
                    source = i;
                }

                if (_vibes[i].Water > bestWater)
                {
                    bestWater = _vibes[i].Water;
                    sink = i;
                }
            }

            if (source < 0 || sink < 0 || source == sink)
                return;
            Vector3 a = _towns[source];
            Vector3 b = _towns[sink];
            Vector3 delta = b - a;
            Vector3 side = Vector3.Cross(Vector3.up, delta.normalized);
            int steps = 5;
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                Vector3 at = Vector3.Lerp(a, b, t) + side * Mathf.Sin(t * Mathf.PI * 1.4f) * 2.4f;
                if (Blocked(at, 2.2f, true))
                    continue;
                Pond(at, Rand(1.6f, 2.4f), Rand(1.1f, 1.7f), RailroadMaterials.ShallowWater, 0.4f);
            }
        }

        private void Pond(Vector3 position, float width, float depth, Material water, float sheen)
        {
            Vector3 flat = Flatten(position);
            if (TownClearance(flat) < 2.1f)
                return;
            var pond = Prim(PrimitiveType.Cylinder, "Water", WorldLayers.Lift(flat, WorldLayers.WaterY(water), _waters.Count), new Vector3(width, WorldLayers.WaterHalf, depth), water);
            _waters.Add(flat);
            Prim(PrimitiveType.Cylinder, "Shallows", WorldLayers.Lift(flat, WorldLayers.Shore, _waters.Count), new Vector3(width * 1.18f, WorldLayers.ShoreHalf, depth * 1.18f), RailroadMaterials.Shore);
            if (sheen > 0.3f)
            {
                var gleam = pond.AddComponent<WaterSheen>();
                gleam.Phase = Rand(0f, 8f);
                gleam.Amount = 0.012f + sheen * 0.012f;
            }
        }

        private void PaintMountains()
        {
            int rim = 7 + _towns.Count;
            for (int i = 0; i < rim; i++)
            {
                float ang = (i / (float)rim) * Mathf.PI * 2f + Rand(-0.12f, 0.12f);
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 at = _center + dir * (_radius + Rand(10f, 16f));
                float alpine = 0.35f + EdgeMountain(at);
                Mountain(at, Rand(7.5f, 12f), Rand(5.5f, 10.5f) * (0.7f + alpine), alpine > 0.55f);
            }

            for (int i = 0; i < _towns.Count; i++)
            {
                if (_vibes[i].Mountain < 0.4f)
                    continue;
                Vector3 at = _towns[i] + Outward(_towns[i]) * Rand(6.5f, 10.5f);
                if (TownClearance(at) < 4.5f)
                    at += Outward(_towns[i]) * 3f;
                Mountain(at, Rand(6f, 9.5f), Rand(4.8f, 8.5f), _vibes[i].Snow > 0.4f || _vibes[i].Mountain > 0.75f);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector3 at = SampleMap(0.75f, 1.35f);
                if (TownClearance(at) < 6f)
                    continue;
                Hillock(at, Rand(4.5f, 7.2f), Rand(1.6f, 2.6f));
            }
        }

        private float EdgeMountain(Vector3 at)
        {
            float best = 0f;
            for (int i = 0; i < _towns.Count; i++)
            {
                float fall = 1f / (1f + Vector3.Distance(Flatten(at), Flatten(_towns[i])) * 0.08f);
                float v = _vibes[i].Mountain * fall;
                if (v > best)
                    best = v;
            }

            return best;
        }

        private void Mountain(Vector3 at, float width, float height, bool snow)
        {
            at.y = height * 0.22f;
            Prim(PrimitiveType.Sphere, "Mountain", at, new Vector3(width, height, width * Rand(0.78f, 1.12f)), RailroadMaterials.Peak);
            Prim(PrimitiveType.Sphere, "MountainShade", at + new Vector3(width * 0.18f, -height * 0.05f, width * 0.1f), new Vector3(width * 0.62f, height * 0.72f, width * 0.58f), RailroadMaterials.PeakShade);
            if (snow)
                Prim(PrimitiveType.Sphere, "Snow", at + Vector3.up * (height * 0.38f), new Vector3(width * 0.28f, height * 0.22f, width * 0.28f), RailroadMaterials.Snow);
            int crags = 3;
            for (int i = 0; i < crags; i++)
            {
                Vector3 r = at + new Vector3(Rand(-width * 0.35f, width * 0.35f), -height * 0.18f, Rand(-width * 0.35f, width * 0.35f));
                Boulder(r, Rand(0.7f, 1.5f), RailroadMaterials.Cliff);
            }
        }

        private void Hillock(Vector3 at, float width, float height)
        {
            at.y = -0.15f;
            Prim(PrimitiveType.Sphere, "Hill", at, new Vector3(width, height, width * Rand(0.8f, 1.15f)), RailroadMaterials.Hill);
        }

        private void PaintForests()
        {
            int trees = 18 + _towns.Count * 10;
            if (trees > 120)
                trees = 120;
            for (int i = 0; i < _towns.Count; i++)
            {
                int grove = 6 + Mathf.RoundToInt(_vibes[i].Forest * 14f);
                Vector3 origin = _towns[i];
                Vector3 outward = Outward(origin);
                for (int n = 0; n < grove; n++)
                {
                    Vector3 dir = (outward + new Vector3(Rand(-1f, 1f), 0f, Rand(-1f, 1f))).normalized;
                    Vector3 at = origin + dir * Rand(3.4f, 8.2f);
                    TryTree(at, _vibes[i]);
                }

                if (_vibes[i].Coast >= 0.5f)
                {
                    for (int n = 0; n < 4; n++)
                    {
                        Vector3 at = origin + Vector3.Cross(Vector3.up, outward).normalized * Rand(-4.5f, 4.5f) + outward * Rand(2.8f, 4.6f);
                        TryTree(at, _vibes[i], true);
                    }
                }
            }

            for (int i = 0; i < trees; i++)
            {
                Vector3 at = SampleMap(0.08f, 1.08f);
                TownVibe vibe = SampleVibe(at);
                if (vibe.Forest < 0.18f && vibe.Coast < 0.35f && Rand01() > 0.35f)
                    continue;
                TryTree(at, vibe);
            }
        }

        private void TryTree(Vector3 at, TownVibe vibe, bool forcePalm = false)
        {
            at = Flatten(at);
            if (Blocked(at, 2.55f, true) || InWater(at, 1.6f))
                return;
            float scale = Rand(0.82f, 1.28f);
            if (forcePalm || (vibe.Coast > 0.55f && vibe.Mountain < 0.4f))
                Palm(at, scale);
            else if (vibe.Mountain > 0.5f || vibe.Snow > 0.35f)
                Pine(at, scale);
            else
                Oak(at, scale);
        }

        private void Oak(Vector3 at, float scale)
        {
            Prim(PrimitiveType.Cylinder, "Trunk", at + Vector3.up * (0.42f * scale), new Vector3(0.16f, 0.42f, 0.16f) * scale, RailroadMaterials.Trunk);
            Prim(PrimitiveType.Sphere, "Canopy", at + Vector3.up * (1.05f * scale), new Vector3(1.15f, 0.95f, 1.15f) * scale, RailroadMaterials.Leaf);
            Prim(PrimitiveType.Sphere, "CanopyDark", at + new Vector3(0.22f, 0.92f, -0.12f) * scale, new Vector3(0.8f, 0.7f, 0.8f) * scale, RailroadMaterials.LeafDark);
        }

        private void Pine(Vector3 at, float scale)
        {
            Prim(PrimitiveType.Cylinder, "Trunk", at + Vector3.up * (0.38f * scale), new Vector3(0.14f, 0.4f, 0.14f) * scale, RailroadMaterials.Trunk);
            Prim(PrimitiveType.Sphere, "PineLow", at + Vector3.up * (0.82f * scale), new Vector3(1.05f, 0.7f, 1.05f) * scale, RailroadMaterials.Pine);
            Prim(PrimitiveType.Sphere, "PineMid", at + Vector3.up * (1.22f * scale), new Vector3(0.78f, 0.62f, 0.78f) * scale, RailroadMaterials.Pine);
            Prim(PrimitiveType.Sphere, "PineTop", at + Vector3.up * (1.58f * scale), new Vector3(0.46f, 0.5f, 0.46f) * scale, RailroadMaterials.LeafDark);
        }

        private void Palm(Vector3 at, float scale)
        {
            Prim(PrimitiveType.Cylinder, "PalmTrunk", at + Vector3.up * (0.72f * scale), new Vector3(0.12f, 0.72f, 0.12f) * scale, RailroadMaterials.Trunk);
            for (int i = 0; i < 5; i++)
            {
                float ang = i / 5f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0.15f, Mathf.Sin(ang));
                var frond = Prim(PrimitiveType.Cube, "Frond", at + Vector3.up * (1.42f * scale) + dir * (0.45f * scale), new Vector3(0.18f, 0.06f, 0.85f) * scale, RailroadMaterials.Frond);
                frond.transform.rotation = Quaternion.LookRotation(dir + Vector3.up * 0.2f);
            }
        }

        private void PaintRocks()
        {
            int count = 14 + _towns.Count * 5;
            if (count > 70)
                count = 70;
            for (int i = 0; i < _towns.Count; i++)
            {
                if (_vibes[i].Rock < 0.3f && _vibes[i].Mountain < 0.45f)
                    continue;
                int pile = 4 + Mathf.RoundToInt(_vibes[i].Rock * 6f);
                for (int n = 0; n < pile; n++)
                {
                    Vector3 at = _towns[i] + new Vector3(Rand(-1f, 1f), 0f, Rand(-1f, 1f)).normalized * Rand(2.8f, 5.8f);
                    TryRock(at, _vibes[i]);
                }
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 at = SampleMap(0.05f, 1.2f);
                TryRock(at, SampleVibe(at));
            }
        }

        private void TryRock(Vector3 at, TownVibe vibe)
        {
            at = Flatten(at);
            if (Blocked(at, 2.2f, true) || InWater(at, 1.3f))
                return;
            var mat = vibe.Coast > 0.4f ? RailroadMaterials.RockWarm : (Rand01() > 0.5f ? RailroadMaterials.Rock : RailroadMaterials.RockDark);
            Boulder(at + Vector3.up * 0.18f, Rand(0.35f, 0.9f), mat);
        }

        private void Boulder(Vector3 at, float size, Material material)
        {
            var go = Prim(PrimitiveType.Cube, "Rock", at, new Vector3(size * Rand(0.8f, 1.3f), size * Rand(0.55f, 1.1f), size * Rand(0.8f, 1.25f)), material);
            go.transform.rotation = Quaternion.Euler(Rand(-18f, 18f), Rand(0f, 360f), Rand(-18f, 18f));
        }

        private void PaintClouds()
        {
            Vector3 min = _center - new Vector3(_radius + 18f, 0f, _radius + 18f);
            Vector3 max = _center + new Vector3(_radius + 18f, 0f, _radius + 18f);
            int count = 6 + Mathf.Min(6, _towns.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 at = new Vector3(Rand(min.x, max.x), Rand(7.5f, 13.5f), Rand(min.z, max.z));
                var puff = new GameObject("Cloud");
                puff.transform.SetParent(_root, false);
                puff.transform.position = at;
                int blobs = 4 + Next(3);
                for (int b = 0; b < blobs; b++)
                {
                    Vector3 offset = new Vector3(Rand(-1.4f, 1.4f), Rand(-0.25f, 0.35f), Rand(-0.9f, 0.9f));
                    float s = Rand(1.6f, 3.2f);
                    Prim(PrimitiveType.Sphere, "Puff", at + offset, new Vector3(s, s * 0.48f, s * 0.82f), b == 0 ? RailroadMaterials.Cloud : RailroadMaterials.CloudShade, puff.transform);
                }

                var drift = puff.AddComponent<CloudDrift>();
                drift.Min = min;
                drift.Max = max;
                drift.Wind = new Vector3(Rand(0.55f, 1.35f), 0f, Rand(-0.12f, 0.28f));
                drift.Phase = Rand(0f, 10f);
                drift.BobAmp = Rand(0.12f, 0.32f);
            }
        }

        private TownVibe SampleVibe(Vector3 at)
        {
            var blend = new TownVibe();
            float weight = 0.0001f;
            for (int i = 0; i < _towns.Count; i++)
            {
                float d = Vector3.Distance(Flatten(at), Flatten(_towns[i]));
                float w = 1f / (1.2f + d * 0.12f);
                blend.Add(_vibes[i], w);
                weight += w;
            }

            blend.Scale(1f / weight);
            return blend;
        }

        private Vector3 SampleMap(float inner, float outer)
        {
            float ang = Rand(0f, Mathf.PI * 2f);
            float r = _radius * Rand(inner, outer);
            return _center + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
        }

        private Vector3 Outward(Vector3 town)
        {
            Vector3 dir = Flatten(town) - Flatten(_center);
            if (dir.sqrMagnitude < 0.2f)
                dir = Vector3.back;
            return dir.normalized;
        }

        private bool Blocked(Vector3 at, float townPad, bool rails)
        {
            if (TownClearance(at) < townPad)
                return true;
            if (!rails)
                return false;
            for (int i = 0; i < _tracks.Count; i++)
            {
                if (DistanceToTrack(at, _tracks[i]) < 1.35f)
                    return true;
            }

            return false;
        }

        private float TownClearance(Vector3 at)
        {
            float best = float.MaxValue;
            for (int i = 0; i < _towns.Count; i++)
            {
                float d = Vector3.Distance(Flatten(at), Flatten(_towns[i]));
                if (d < best)
                    best = d;
            }

            return best;
        }

        private bool InWater(Vector3 at, float pad)
        {
            for (int i = 0; i < _waters.Count; i++)
            {
                if (Vector3.Distance(Flatten(at), _waters[i]) < pad)
                    return true;
            }

            return false;
        }

        private static float DistanceToTrack(Vector3 p, Track track)
        {
            Vector3 a = Flatten(track.A);
            Vector3 b = Flatten(track.B);
            Vector3 pt = Flatten(p);
            Vector3 ab = b - a;
            float mag = ab.sqrMagnitude;
            float t = mag < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(pt - a, ab) / mag);
            return Vector3.Distance(pt, a + ab * t);
        }

        private static Vector3 Flatten(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : _root, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        private float Rand01()
        {
            _rng = (int)(_rng * 1664525L + 1013904223);
            return ((_rng >> 8) & 0xFFFFFF) / 16777215f;
        }

        private float Rand(float a, float b) => a + (b - a) * Rand01();

        private int Next(int maxExclusive) => (int)(Rand01() * maxExclusive);

        private static int StableSeed(RailroadMap map)
        {
            int hash = 17;
            hash = Mix(hash, map.Title);
            hash = Mix(hash, map.StartTownId);
            for (int i = 0; i < map.Towns.Count; i++)
            {
                hash = Mix(hash, map.Towns[i].Id);
                hash = Mix(hash, map.Towns[i].Name);
            }

            return hash == 0 ? 7 : hash;
        }

        private static int Mix(int hash, string text)
        {
            if (text == null)
                return hash * 31;
            for (int i = 0; i < text.Length; i++)
                hash = hash * 31 + text[i];
            return hash;
        }

        private struct Track
        {
            public Vector3 A;
            public Vector3 B;
        }

        private sealed class TownVibe
        {
            public float Water;
            public float Forest;
            public float Mountain;
            public float Rock;
            public float Coast;
            public float Marsh;
            public float Island;
            public float Snow;
            public float City;

            public static TownVibe From(TownDef town, IReadOnlyList<LandmarkDef> landmarks, float northness)
            {
                string key = ((town.Id ?? "") + " " + (town.Name ?? "")).ToLowerInvariant();
                var vibe = new TownVibe();
                vibe.Water = Score(key, "lake", "pond", "pool", "bay", "port", "harbour", "harbor", "sea", "ocean", "river", "creek", "cove", "beach", "mere", "diego", "harbor");
                vibe.Coast = Score(key, "port", "beach", "coast", "clemente", "diego", "venice", "bay", "sand", "isle", "island", "cape");
                vibe.Forest = Score(key, "wood", "forest", "copse", "grove", "willow", "pine", "cedar", "oak", "tree", "green", "outskirt", "park", "glade");
                vibe.Mountain = Score(key, "mount", "peak", "spire", "crest", "ridge", "hill", "pass", "alps", "summit", "bluff");
                vibe.Rock = Score(key, "stone", "rock", "cliff", "quarry", "crag", "canyon", "gorge", "ember", "mesa");
                vibe.Marsh = Score(key, "marsh", "salt", "swamp", "bog", "fen", "wet");
                vibe.Island = Score(key, "island", "isle", "key", "atoll");
                vibe.Snow = Score(key, "north", "ice", "frost", "snow", "winter", "spire");
                vibe.City = Score(key, "york", "city", "burg", "downtown", "metro");
                vibe.Mountain += northness * 0.35f;
                if (vibe.Island > 0.4f)
                {
                    vibe.Water = Mathf.Max(vibe.Water, 0.8f);
                    vibe.Coast = Mathf.Max(vibe.Coast, 0.7f);
                }

                if (vibe.Coast > 0.4f)
                    vibe.Water = Mathf.Max(vibe.Water, 0.55f);
                if (vibe.Marsh > 0.4f)
                    vibe.Water = Mathf.Max(vibe.Water, 0.5f);
                if (vibe.City > 0.4f)
                    vibe.Forest *= 0.35f;
                if (landmarks != null)
                {
                    for (int i = 0; i < landmarks.Count; i++)
                    {
                        if (landmarks[i].TownId != town.Id)
                            continue;
                        if (landmarks[i].Kind == LandmarkDef.Lake)
                            vibe.Water = Mathf.Max(vibe.Water, 1f);
                        if (landmarks[i].Kind == LandmarkDef.Marsh)
                        {
                            vibe.Marsh = Mathf.Max(vibe.Marsh, 1f);
                            vibe.Water = Mathf.Max(vibe.Water, 0.7f);
                        }
                    }
                }

                if (vibe.Forest < 0.2f && vibe.City < 0.4f && vibe.Coast < 0.4f)
                    vibe.Forest = 0.28f;
                return vibe;
            }

            public void Add(TownVibe other, float w)
            {
                Water += other.Water * w;
                Forest += other.Forest * w;
                Mountain += other.Mountain * w;
                Rock += other.Rock * w;
                Coast += other.Coast * w;
                Marsh += other.Marsh * w;
                Island += other.Island * w;
                Snow += other.Snow * w;
                City += other.City * w;
            }

            public void Scale(float s)
            {
                Water *= s;
                Forest *= s;
                Mountain *= s;
                Rock *= s;
                Coast *= s;
                Marsh *= s;
                Island *= s;
                Snow *= s;
                City *= s;
            }

            private static float Score(string key, params string[] words)
            {
                float best = 0f;
                for (int i = 0; i < words.Length; i++)
                {
                    if (key.IndexOf(words[i], StringComparison.Ordinal) < 0)
                        continue;
                    float hit = 0.7f + words[i].Length * 0.04f;
                    if (hit > best)
                        best = hit;
                }

                return Mathf.Clamp01(best);
            }
        }
    }
}
