namespace Knightworld.Core
{
    public enum CoverLevel
    {
        None = 0,
        Half = 1,
        ThreeQuarter = 2,
        Wall = 3
    }

    public enum Cardinal
    {
        North,
        East,
        South,
        West
    }

    public static class CoverRules
    {
        public static int ArmorBonus(CoverLevel cover)
        {
            switch (cover)
            {
                case CoverLevel.Half: return 2;
                case CoverLevel.ThreeQuarter: return 5;
                default: return 0;
            }
        }

        public static CoverLevel Max(CoverLevel a, CoverLevel b) => a > b ? a : b;

        public static string Label(CoverLevel cover)
        {
            switch (cover)
            {
                case CoverLevel.Half: return "half cover +2";
                case CoverLevel.ThreeQuarter: return "three-quarters cover +5";
                case CoverLevel.Wall: return "total cover";
                default: return "no cover";
            }
        }

        public static Cardinal Opposite(Cardinal dir)
        {
            switch (dir)
            {
                case Cardinal.North: return Cardinal.South;
                case Cardinal.East: return Cardinal.West;
                case Cardinal.South: return Cardinal.North;
                default: return Cardinal.East;
            }
        }

        public static GridPos Step(Cardinal dir)
        {
            switch (dir)
            {
                case Cardinal.North: return new GridPos(0, 1);
                case Cardinal.East: return new GridPos(1, 0);
                case Cardinal.South: return new GridPos(0, -1);
                default: return new GridPos(-1, 0);
            }
        }
    }
}
