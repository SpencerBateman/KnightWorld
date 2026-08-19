using System;

namespace Knightworld.Core
{
    public sealed class SeededRandom : IRandom
    {
        public int State { get; private set; }

        public SeededRandom(int seed)
        {
            State = seed == 0 ? 1103515245 : seed;
        }

        public void Restore(int state)
        {
            State = state == 0 ? 1103515245 : state;
        }

        public int NextInclusive(int min, int max)
        {
            if (max < min)
                throw new ArgumentOutOfRangeException(nameof(max));
            State = (int)(State * 1664525L + 1013904223);
            int span = max - min + 1;
            uint sample = (uint)State;
            return min + (int)(sample % (uint)span);
        }
    }
}
