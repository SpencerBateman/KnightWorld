using System;

namespace Knightworld.Core
{
    public sealed class SeededRandom : IRandom
    {
        private readonly Random _random;

        public SeededRandom(int seed)
        {
            _random = new Random(seed);
        }

        public int NextInclusive(int min, int max)
        {
            if (max < min)
                throw new ArgumentOutOfRangeException(nameof(max));
            return _random.Next(min, max + 1);
        }
    }
}
