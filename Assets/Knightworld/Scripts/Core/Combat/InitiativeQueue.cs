using System.Collections.Generic;

namespace Knightworld.Core
{
    public readonly struct InitiativeEntry
    {
        public int UnitId { get; }
        public int Roll { get; }
        public int Bonus { get; }
        public int Total => Roll + Bonus;

        public InitiativeEntry(int unitId, int roll, int bonus)
        {
            UnitId = unitId;
            Roll = roll;
            Bonus = bonus;
        }
    }

    public sealed class InitiativeQueue
    {
        private readonly List<InitiativeEntry> _order = new List<InitiativeEntry>();
        private int _index;

        public IReadOnlyList<InitiativeEntry> Order => _order;
        public int Index => _index;
        public int Round { get; private set; } = 1;

        public int CurrentUnitId => _order.Count == 0 ? 0 : _order[_index].UnitId;

        public void Roll(IReadOnlyList<UnitState> units, IRandom rng)
        {
            _order.Clear();
            _index = 0;
            Round = 1;
            foreach (var unit in units)
            {
                if (unit.IsDead)
                    continue;
                int roll = rng.NextInclusive(1, 20);
                _order.Add(new InitiativeEntry(unit.Id, roll, unit.InitiativeBonus));
            }

            _order.Sort(Compare);
        }

        public bool AdvanceToNextLiving(IReadOnlyList<UnitState> units)
        {
            if (_order.Count == 0)
                return false;

            for (int attempts = 0; attempts < _order.Count; attempts++)
            {
                _index++;
                if (_index >= _order.Count)
                {
                    _index = 0;
                    Round++;
                }

                var unit = Find(units, CurrentUnitId);
                if (unit != null && !unit.IsDead)
                    return true;
            }

            return false;
        }

        public IReadOnlyList<int> LivingOrder(IReadOnlyList<UnitState> units)
        {
            var ids = new List<int>();
            foreach (var entry in _order)
            {
                var unit = Find(units, entry.UnitId);
                if (unit != null && !unit.IsDead)
                    ids.Add(entry.UnitId);
            }

            return ids;
        }

        private static int Compare(InitiativeEntry a, InitiativeEntry b)
        {
            int total = b.Total.CompareTo(a.Total);
            if (total != 0)
                return total;
            int bonus = b.Bonus.CompareTo(a.Bonus);
            if (bonus != 0)
                return bonus;
            return a.UnitId.CompareTo(b.UnitId);
        }

        private static UnitState Find(IReadOnlyList<UnitState> units, int id)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Id == id)
                    return units[i];
            }

            return null;
        }
    }
}
