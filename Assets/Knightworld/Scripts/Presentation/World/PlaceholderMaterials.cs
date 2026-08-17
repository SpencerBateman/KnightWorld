using UnityEngine;

namespace Knightworld.Presentation
{
    public static class PlaceholderMaterials
    {
        public static Material FloorA { get; private set; }
        public static Material FloorB { get; private set; }
        public static Material GrassA { get; private set; }
        public static Material GrassB { get; private set; }
        public static Material GrassField { get; private set; }
        public static Material WaterA { get; private set; }
        public static Material WaterB { get; private set; }
        public static Material Bark { get; private set; }
        public static Material Leaves { get; private set; }
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
        public static Material OverworldGrass { get; private set; }
        public static Material OverworldHill { get; private set; }
        public static Material OverworldDirt { get; private set; }
        public static Material OverworldWater { get; private set; }
        public static Material OverworldStone { get; private set; }
        public static Material OverworldLocked { get; private set; }
        public static Material OverworldOpen { get; private set; }
        public static Material OverworldClear { get; private set; }
        public static Material OverworldCurrent { get; private set; }
        public static Material OverworldToken { get; private set; }

        public static void Ensure()
        {
            if (FloorA != null)
                return;
            FloorA = Create(new Color(0.28f, 0.26f, 0.24f));
            FloorB = Create(new Color(0.34f, 0.31f, 0.28f));
            GrassA = Create(new Color(0.34f, 0.56f, 0.24f));
            GrassB = Create(new Color(0.40f, 0.63f, 0.28f));
            GrassField = Create(new Color(0.26f, 0.46f, 0.18f));
            WaterA = Create(new Color(0.18f, 0.42f, 0.62f));
            WaterB = Create(new Color(0.22f, 0.50f, 0.70f));
            Bark = Create(new Color(0.32f, 0.20f, 0.10f));
            Leaves = Create(new Color(0.16f, 0.38f, 0.14f));
            Wall = Create(new Color(0.42f, 0.40f, 0.37f));
            CoverHalf = Create(new Color(0.45f, 0.38f, 0.22f));
            CoverThreeQuarter = Create(new Color(0.38f, 0.32f, 0.20f));
            Player = Create(new Color(0.25f, 0.45f, 0.85f));
            Enemy = Create(new Color(0.72f, 0.22f, 0.18f));
            Dead = Create(new Color(0.25f, 0.25f, 0.25f));
            Reachable = Create(new Color(0.20f, 0.55f, 1f, 0.72f), true, 3000);
            Path = Create(new Color(1f, 0.85f, 0.15f, 0.85f), true, 3001);
            Hover = Create(new Color(1f, 1f, 1f, 0.9f), true, 3002);
            Attack = Create(new Color(0.95f, 0.22f, 0.15f, 0.7f), true, 3001);
            Active = Create(new Color(0.95f, 0.9f, 0.35f));
            SelectedPlayer = Create(new Color(0.35f, 0.85f, 1f, 0.9f), true, 3003);
            SelectedEnemy = Create(new Color(1f, 0.45f, 0.2f, 0.9f), true, 3003);
            OverworldGrass = Create(new Color(0.32f, 0.62f, 0.26f));
            OverworldHill = Create(new Color(0.28f, 0.52f, 0.22f));
            OverworldDirt = Create(new Color(0.72f, 0.55f, 0.28f));
            OverworldWater = Create(new Color(0.20f, 0.48f, 0.72f));
            OverworldStone = Create(new Color(0.48f, 0.46f, 0.42f));
            OverworldLocked = Create(new Color(0.38f, 0.38f, 0.40f));
            OverworldOpen = Create(new Color(0.92f, 0.78f, 0.28f));
            OverworldClear = Create(new Color(0.35f, 0.78f, 0.42f));
            OverworldCurrent = Create(new Color(1f, 0.92f, 0.45f));
            OverworldToken = Create(new Color(0.22f, 0.42f, 0.88f));
        }

        private static Material Create(Color color, bool transparent = false, int renderQueue = -1)
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
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                material.renderQueue = renderQueue > 0 ? renderQueue : (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetOverrideTag("RenderType", "Transparent");
            }

            return material;
        }
    }
}
