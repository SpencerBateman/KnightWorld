namespace Knightworld.Core
{
    public readonly struct DiceFormula
    {
        public int Count { get; }
        public int Sides { get; }
        public int Bonus { get; }

        public DiceFormula(int count, int sides, int bonus)
        {
            Count = count;
            Sides = sides;
            Bonus = bonus;
        }

        public int Roll(IRandom rng, bool critical = false)
        {
            int dice = critical ? Count * 2 : Count;
            int total = Bonus;
            for (int i = 0; i < dice; i++)
                total += rng.NextInclusive(1, Sides);
            return total;
        }

        public float Average => Count * (Sides + 1) / 2f + Bonus;

        public override string ToString()
        {
            if (Bonus > 0)
                return $"{Count}d{Sides}+{Bonus}";
            if (Bonus < 0)
                return $"{Count}d{Sides}{Bonus}";
            return $"{Count}d{Sides}";
        }
    }
}
