using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public sealed class SequenceRandom : IRandom
    {
        private readonly Queue<int> _values;

        public SequenceRandom(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public int NextInclusive(int min, int max)
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("SequenceRandom has no values left.");
            return _values.Dequeue();
        }
    }
}
