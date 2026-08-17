using System.Collections.Generic;

namespace Knightworld.Core
{
    public sealed class Passenger
    {
        public int Id { get; }
        public string Name { get; }
        public string OriginId { get; }
        public string DestId { get; }

        public Passenger(int id, string name, string originId, string destId)
        {
            Id = id;
            Name = name;
            OriginId = originId;
            DestId = destId;
        }
    }

    public sealed class RailSession
    {
        public const int SeatCount = 10;
        public const int MaxWaitingPerTown = 4;
        public static readonly string[] Names =
        {
            "Ada", "Bram", "Cora", "Dax", "Elia", "Flint", "Gita", "Holt", "Ines", "Joss",
            "Kade", "Lina", "Moss", "Nyla", "Otto", "Pia", "Quinn", "Rusk", "Sable", "Tess"
        };

        private readonly IRandom _random;
        private int _nextPassengerId = 1;

        public int Score { get; private set; }
        public string CurrentTownId { get; private set; }
        public List<Passenger> Onboard { get; } = new List<Passenger>();
        public Dictionary<string, List<Passenger>> Waiting { get; } = new Dictionary<string, List<Passenger>>();

        public int FreeSeats => SeatCount - Onboard.Count;

        public RailSession(IRandom random, string startTownId)
        {
            _random = random;
            CurrentTownId = startTownId;
            for (int i = 0; i < RailroadGraph.Towns.Count; i++)
                Waiting[RailroadGraph.Towns[i].Id] = new List<Passenger>();
        }

        public void SeedWaiting(int perTown)
        {
            for (int t = 0; t < RailroadGraph.Towns.Count; t++)
            {
                for (int n = 0; n < perTown; n++)
                    TrySpawnAt(RailroadGraph.Towns[t].Id);
            }
        }

        public bool TrySpawnPassenger()
        {
            var open = new List<string>();
            for (int i = 0; i < RailroadGraph.Towns.Count; i++)
            {
                string id = RailroadGraph.Towns[i].Id;
                if (Waiting[id].Count < MaxWaitingPerTown)
                    open.Add(id);
            }

            if (open.Count == 0)
                return false;
            return TrySpawnAt(open[_random.NextInclusive(0, open.Count - 1)]);
        }

        public bool TrySpawnAt(string townId)
        {
            if (!Waiting.ContainsKey(townId) || Waiting[townId].Count >= MaxWaitingPerTown)
                return false;
            string dest = RandomOtherTown(townId);
            string name = Names[_random.NextInclusive(0, Names.Length - 1)];
            Waiting[townId].Add(new Passenger(_nextPassengerId++, name, townId, dest));
            return true;
        }

        public int Arrive(string townId)
        {
            CurrentTownId = townId;
            int delivered = 0;
            for (int i = Onboard.Count - 1; i >= 0; i--)
            {
                if (Onboard[i].DestId != townId)
                    continue;
                Onboard.RemoveAt(i);
                delivered++;
            }

            Score += delivered;
            return delivered;
        }

        public bool TryBoard(int passengerId)
        {
            if (Onboard.Count >= SeatCount)
                return false;
            var waiting = Waiting[CurrentTownId];
            for (int i = 0; i < waiting.Count; i++)
            {
                if (waiting[i].Id != passengerId)
                    continue;
                Onboard.Add(waiting[i]);
                waiting.RemoveAt(i);
                return true;
            }

            return false;
        }

        public Passenger FindWaiting(int passengerId)
        {
            var waiting = Waiting[CurrentTownId];
            for (int i = 0; i < waiting.Count; i++)
            {
                if (waiting[i].Id == passengerId)
                    return waiting[i];
            }

            return null;
        }

        public IReadOnlyList<Passenger> WaitingHere => Waiting[CurrentTownId];

        private string RandomOtherTown(string originId)
        {
            int index = _random.NextInclusive(0, RailroadGraph.Towns.Count - 2);
            for (int i = 0; i < RailroadGraph.Towns.Count; i++)
            {
                if (RailroadGraph.Towns[i].Id == originId)
                    continue;
                if (index == 0)
                    return RailroadGraph.Towns[i].Id;
                index--;
            }

            return RailroadGraph.Towns[0].Id;
        }
    }
}
