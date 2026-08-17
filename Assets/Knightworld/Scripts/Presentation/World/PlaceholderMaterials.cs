using UnityEngine;

namespace Knightworld.Presentation
{
    public static class PlaceholderMaterials
    {
        public static Material FloorA { get; private set; }
        public static Material FloorB { get; private set; }
        public static Material Wall { get; private set; }
        public static Material CoverHalf { get; private set; }
        public static Material CoverThreeQuarter { get; private set; }
        public static Material Player { get; private set; }
        public static Material Enemy { get; private set; }
        public static Material Dead { get; private set; }
        public static Material Reachable { get; private set; }
        public static Material Path { get; private set; }
        public static Material Hover { get; private set; }
        public static Material Attack { get; private set; }
        public static Material Active { get; private set; }
        public static Material SelectedPlayer { get; private set; }
        public static Material SelectedEnemy { get; private set; }

        public static void Ensure()
        {
            if (FloorA != null)
                return;
            FloorA = Create(new Color(0.28f, 0.26f, 0.24f));
            FloorB = Create(new Color(0.34f, 0.31f, 0.28f));
            Wall = Create(new Color(0.16f, 0.15f, 0.14f));
            CoverHalf = Create(new Color(0.45f, 0.38f, 0.22f));
            CoverThreeQuarter = Create(new Color(0.38f, 0.32f, 0.20f));
            Player = Create(new Color(0.25f, 0.45f, 0.85f));
            Enemy = Create(new Color(0.72f, 0.22f, 0.18f));
            Dead = Create(new Color(0.25f, 0.25f, 0.25f));
            Reachable = Create(new Color(0.20f, 0.55f, 0.85f, 0.55f), true);
            Path = Create(new Color(0.95f, 0.82f, 0.25f, 0.7f), true);
            Hover = Create(new Color(1f, 1f, 1f, 0.8f), true);
            Attack = Create(new Color(0.9f, 0.2f, 0.15f, 0.55f), true);
            Active = Create(new Color(0.95f, 0.9f, 0.35f));
            SelectedPlayer = Create(new Color(0.35f, 0.85f, 1f, 0.9f), true);
            SelectedEnemy = Create(new Color(1f, 0.45f, 0.2f, 0.9f), true);
        }

        private static Material Create(Color color, bool transparent = false)
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
            if (transparent && material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            return material;
        }
    }
}
