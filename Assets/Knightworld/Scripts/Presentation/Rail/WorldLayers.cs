using UnityEngine;

namespace Knightworld.Presentation
{
    /// <summary>
    /// World-Y and draw-order stacking so coplanar scenery does not z-fight.
    /// Y values are mesh centers. Lower sits underneath; tracks always sit above water.
    /// </summary>
    public static class WorldLayers
    {
        public const float Earth = -0.12f;
        public const float Grass = 0f;
        public const float Meadow = 0.028f;
        public const float Sand = 0.048f;
        public const float Shore = 0.068f;
        public const float Marsh = 0.086f;
        public const float Shallow = 0.096f;
        public const float Water = 0.102f;
        public const float Deep = 0.110f;
        public const float Ballast = 0.150f;
        public const float Tie = 0.190f;
        public const float Rail = 0.232f;
        public const float Platform = 0.318f;

        public static float PadTop => Platform + PlatformHalf;
        public static float House => PadTop + 0.29f;
        public static float HouseLow => PadTop + 0.22f;
        public static float Store => PadTop + 0.26f;
        public static float Person => PadTop + 0.29f;

        public const float MeadowHalf = 0.01f;
        public const float SandHalf = 0.01f;
        public const float ShoreHalf = 0.012f;
        public const float WaterHalf = 0.014f;
        public const float BallastThick = 0.04f;
        public const float TieThick = 0.04f;
        public const float RailThick = 0.05f;
        public const float PlatformHalf = 0.055f;

        public const int QueueEarth = 1990;
        public const int QueueGrass = 2000;
        public const int QueueMeadow = 2002;
        public const int QueueSand = 2004;
        public const int QueueShore = 2006;
        public const int QueueMarsh = 2008;
        public const int QueueShallow = 2010;
        public const int QueueWater = 2012;
        public const int QueueDeep = 2014;
        public const int QueueBallast = 2016;
        public const int QueueTie = 2018;
        public const int QueueRail = 2020;
        public const int QueuePlatform = 2022;

        public static Vector3 Lift(Vector3 xz, float y)
        {
            return new Vector3(xz.x, y, xz.z);
        }

        public static Vector3 Lift(Vector3 xz, float y, int salt)
        {
            int n = (salt * 1103515245 + 12345) & 0x3ff;
            return new Vector3(xz.x, y + n * (0.004f / 1023f), xz.z);
        }

        public static float WaterY(Material water)
        {
            if (water == RailroadMaterials.MarshWater)
                return Marsh;
            if (water == RailroadMaterials.ShallowWater)
                return Shallow;
            if (water == RailroadMaterials.DeepWater)
                return Deep;
            if (water == RailroadMaterials.Shore)
                return Shore;
            return Water;
        }

        public static void Queue(Material material, int renderQueue)
        {
            if (material != null)
                material.renderQueue = renderQueue;
        }
    }
}
