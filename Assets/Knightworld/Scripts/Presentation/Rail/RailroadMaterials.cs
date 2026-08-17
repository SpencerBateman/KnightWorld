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
        public static Material Millhaven { get; private set; }
        public static Material Lakeside { get; private set; }
        public static Material Hillcrest { get; private set; }
        public static Material Emberford { get; private set; }
        public static Material Portmere { get; private set; }

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
            Millhaven = Make(new Color(0.82f, 0.70f, 0.28f));
            Lakeside = Make(new Color(0.25f, 0.52f, 0.82f));
            Hillcrest = Make(new Color(0.32f, 0.68f, 0.38f));
            Emberford = Make(new Color(0.78f, 0.32f, 0.18f));
            Portmere = Make(new Color(0.28f, 0.70f, 0.68f));
        }

        public static Material Town(string townId)
        {
            Ensure();
            switch (townId)
            {
                case Knightworld.Core.RailroadGraph.Lakeside: return Lakeside;
                case Knightworld.Core.RailroadGraph.Hillcrest: return Hillcrest;
                case Knightworld.Core.RailroadGraph.Emberford: return Emberford;
                case Knightworld.Core.RailroadGraph.Portmere: return Portmere;
                default: return Millhaven;
            }
        }

        private static Material Make(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Hidden/InternalErrorShader");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            return material;
        }
    }
}
