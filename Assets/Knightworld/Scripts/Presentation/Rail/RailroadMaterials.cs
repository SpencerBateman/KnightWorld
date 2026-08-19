using UnityEngine;

namespace Knightworld.Presentation
{
    public static class RailroadMaterials
    {
        public static Material Grass { get; private set; }
        public static Material Hill { get; private set; }
        public static Material Water { get; private set; }
        public static Material Rail { get; private set; }
        public static Material Tie { get; private set; }
        public static Material Train { get; private set; }
        public static Material TrainDark { get; private set; }
        public static Material SeatEmpty { get; private set; }
        public static Material Shop { get; private set; }
        public static Material Meadow { get; private set; }
        public static Material DeepGrass { get; private set; }
        public static Material Earth { get; private set; }
        public static Material ShallowWater { get; private set; }
        public static Material DeepWater { get; private set; }
        public static Material MarshWater { get; private set; }
        public static Material Sand { get; private set; }
        public static Material Shore { get; private set; }
        public static Material Trunk { get; private set; }
        public static Material Leaf { get; private set; }
        public static Material LeafDark { get; private set; }
        public static Material Pine { get; private set; }
        public static Material Frond { get; private set; }
        public static Material Rock { get; private set; }
        public static Material RockWarm { get; private set; }
        public static Material RockDark { get; private set; }
        public static Material Peak { get; private set; }
        public static Material PeakShade { get; private set; }
        public static Material Snow { get; private set; }
        public static Material Cloud { get; private set; }
        public static Material CloudShade { get; private set; }
        public static Material Cliff { get; private set; }
        public static Material Sun { get; private set; }
        public static Material Haze { get; private set; }
        public static Material Gravel { get; private set; }
        public static Material LockedRail { get; private set; }
        private static readonly System.Collections.Generic.Dictionary<string, Material> ExtraTowns =
            new System.Collections.Generic.Dictionary<string, Material>();
        public static Material Millhaven { get; private set; }
        public static Material Lakeside { get; private set; }
        public static Material Hillcrest { get; private set; }
        public static Material Emberford { get; private set; }
        public static Material Portmere { get; private set; }
        public static Material Willowgate { get; private set; }
        public static Material Saltmarsh { get; private set; }
        public static Material Copsewood { get; private set; }
        public static Material Northspire { get; private set; }
        public static Material Stonebridge { get; private set; }

        public static void Ensure()
        {
            if (Grass != null)
                return;
            Grass = Make(new Color(0.36f, 0.58f, 0.28f));
            Hill = Make(new Color(0.30f, 0.50f, 0.24f));
            Water = Make(new Color(0.22f, 0.50f, 0.70f));
            Rail = Make(new Color(0.22f, 0.22f, 0.24f));
            Tie = Make(new Color(0.38f, 0.26f, 0.14f));
            Train = Make(new Color(0.78f, 0.18f, 0.16f));
            TrainDark = Make(new Color(0.18f, 0.18f, 0.20f));
            SeatEmpty = Make(new Color(0.42f, 0.42f, 0.44f));
            Shop = Make(new Color(0.52f, 0.34f, 0.18f));
            Meadow = Make(new Color(0.55f, 0.72f, 0.38f));
            DeepGrass = Make(new Color(0.28f, 0.50f, 0.26f));
            Earth = Make(new Color(0.42f, 0.34f, 0.22f));
            ShallowWater = Make(new Color(0.38f, 0.72f, 0.78f));
            DeepWater = Make(new Color(0.18f, 0.48f, 0.68f));
            MarshWater = Make(new Color(0.32f, 0.52f, 0.38f));
            Sand = Make(new Color(0.84f, 0.76f, 0.55f));
            Shore = Make(new Color(0.62f, 0.78f, 0.72f));
            Trunk = Make(new Color(0.38f, 0.24f, 0.14f));
            Leaf = Make(new Color(0.30f, 0.62f, 0.32f));
            LeafDark = Make(new Color(0.18f, 0.42f, 0.24f));
            Pine = Make(new Color(0.16f, 0.38f, 0.28f));
            Frond = Make(new Color(0.22f, 0.58f, 0.30f));
            Rock = Make(new Color(0.55f, 0.52f, 0.48f));
            RockWarm = Make(new Color(0.62f, 0.50f, 0.40f));
            RockDark = Make(new Color(0.36f, 0.34f, 0.32f));
            Peak = Make(new Color(0.42f, 0.48f, 0.44f));
            PeakShade = Make(new Color(0.32f, 0.36f, 0.34f));
            Snow = Make(new Color(0.92f, 0.95f, 0.97f));
            Cloud = Make(new Color(0.95f, 0.96f, 0.98f));
            CloudShade = Make(new Color(0.82f, 0.86f, 0.90f));
            Cliff = Make(new Color(0.48f, 0.42f, 0.38f));
            Sun = Make(new Color(1f, 0.92f, 0.62f));
            Haze = Make(new Color(0.70f, 0.80f, 0.72f));
            Gravel = Make(new Color(0.50f, 0.42f, 0.32f));
            LockedRail = Make(new Color(0.28f, 0.30f, 0.34f));
            Millhaven = Make(new Color(0.82f, 0.70f, 0.28f));
            Lakeside = Make(new Color(0.25f, 0.52f, 0.82f));
            Hillcrest = Make(new Color(0.32f, 0.68f, 0.38f));
            Emberford = Make(new Color(0.78f, 0.32f, 0.18f));
            Portmere = Make(new Color(0.28f, 0.70f, 0.68f));
            Willowgate = Make(new Color(0.62f, 0.38f, 0.78f));
            Saltmarsh = Make(new Color(0.78f, 0.68f, 0.38f));
            Copsewood = Make(new Color(0.22f, 0.48f, 0.24f));
            Northspire = Make(new Color(0.72f, 0.82f, 0.90f));
            Stonebridge = Make(new Color(0.58f, 0.56f, 0.52f));
            StackQueues();
        }

        public static Material Town(string townId)
        {
            Ensure();
            switch (townId)
            {
                case Knightworld.Core.RailroadGraph.Millhaven: return Millhaven;
                case Knightworld.Core.RailroadGraph.Lakeside: return Lakeside;
                case Knightworld.Core.RailroadGraph.Hillcrest: return Hillcrest;
                case Knightworld.Core.RailroadGraph.Emberford: return Emberford;
                case Knightworld.Core.RailroadGraph.Portmere: return Portmere;
                case Knightworld.Core.RailroadGraph.Willowgate: return Willowgate;
                case Knightworld.Core.RailroadGraph.Saltmarsh: return Saltmarsh;
                case Knightworld.Core.RailroadGraph.Copsewood: return Copsewood;
                case Knightworld.Core.RailroadGraph.Northspire: return Northspire;
                case Knightworld.Core.RailroadGraph.Stonebridge: return Stonebridge;
                default: return ExtraTown(townId);
            }
        }

        public static Color TownColor(string townId)
        {
            var material = Town(townId);
            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");
            return material.color;
        }

        private static Material ExtraTown(string townId)
        {
            if (string.IsNullOrEmpty(townId))
                return Millhaven;
            if (ExtraTowns.TryGetValue(townId, out var material))
                return material;
            float hue = ((townId.GetHashCode() & int.MaxValue) % 360) / 360f;
            material = Make(Color.HSVToRGB(hue, 0.55f, 0.82f));
            WorldLayers.Queue(material, WorldLayers.QueuePlatform);
            ExtraTowns[townId] = material;
            return material;
        }

        private static void StackQueues()
        {
            WorldLayers.Queue(Earth, WorldLayers.QueueEarth);
            WorldLayers.Queue(Haze, WorldLayers.QueueEarth);
            WorldLayers.Queue(Grass, WorldLayers.QueueGrass);
            WorldLayers.Queue(Hill, WorldLayers.QueueMeadow);
            WorldLayers.Queue(Meadow, WorldLayers.QueueMeadow);
            WorldLayers.Queue(DeepGrass, WorldLayers.QueueMeadow);
            WorldLayers.Queue(Sand, WorldLayers.QueueSand);
            WorldLayers.Queue(Shore, WorldLayers.QueueShore);
            WorldLayers.Queue(MarshWater, WorldLayers.QueueMarsh);
            WorldLayers.Queue(ShallowWater, WorldLayers.QueueShallow);
            WorldLayers.Queue(Water, WorldLayers.QueueWater);
            WorldLayers.Queue(DeepWater, WorldLayers.QueueDeep);
            WorldLayers.Queue(Gravel, WorldLayers.QueueBallast);
            WorldLayers.Queue(LockedRail, WorldLayers.QueueBallast);
            WorldLayers.Queue(Tie, WorldLayers.QueueTie);
            WorldLayers.Queue(Rail, WorldLayers.QueueRail);
            WorldLayers.Queue(Millhaven, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Lakeside, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Hillcrest, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Emberford, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Portmere, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Willowgate, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Saltmarsh, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Copsewood, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Northspire, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Stonebridge, WorldLayers.QueuePlatform);
            WorldLayers.Queue(Shop, WorldLayers.QueuePlatform);
        }

        private static Material Make(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Hidden/InternalErrorShader");
            var material = new Material(shader) { color = color };
            material.enableInstancing = true;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            return material;
        }
    }
}
