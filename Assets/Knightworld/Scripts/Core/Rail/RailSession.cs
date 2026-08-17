using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public sealed class Passenger
    {
        public int Id { get; }
        public string Name { get; }
        public string OriginId { get; private set; }
        public string DestId { get; }
        public int Fare { get; private set; }

        public Passenger(int id, string name, string originId, string destId)
        {
            Id = id;
            Name = name;
            OriginId = originId;
            DestId = destId;
            Fare = FareBetween(originId, destId);
        }

        public void Relocate(string townId)
        {
            OriginId = townId;
            Fare = FareBetween(townId, DestId);
        }

        public static int FareBetween(string fromId, string toId)
        {
            if (fromId == toId)
                return 1;
            float distance = RailroadGraph.RouteDistance(RailroadGraph.FindRoute(fromId, toId));
            int fare = (int)Math.Round(distance);
            return fare < 1 ? 1 : fare;
        }
    }

    public sealed class DestinationTally
    {
        public string TownId { get; }
        public string TownName { get; }
        public int Count { get; }

        public DestinationTally(string townId, string townName, int count)
        {
            TownId = townId;
            TownName = townName;
            Count = count;
        }
    }

    public sealed class RailSession
    {
        public const int StartingSeats = 1;
        public const int SeatUpgradeCost = 50;
        public const int SeatUpgradeSeats = 1;
        public const int SeatUpgradeStock = 2;
        public const int CarriageCost = 350;
        public const int CarriageSeats = 6;
        public const int MaxWaitingPerTown = 4;
        public static readonly string[] Names =
        {
            "Ada", "Bram", "Cora", "Dax", "Elia", "Flint", "Gita", "Holt", "Ines", "Joss",
            "Kade", "Lina", "Moss", "Nyla", "Otto", "Pia", "Quinn", "Rusk", "Sable", "Tess"
        };

        private readonly IRandom _random;
        private int _nextPassengerId = 1;

        public int Score { get; private set; }
        public int SeatCount { get; private set; } = StartingSeats;
        public int SeatUpgradesBought { get; private set; }
        public int CarriagesBought { get; private set; }
        public string CurrentTownId { get; private set; }
        public List<Passenger> Onboard { get; } = new List<Passenger>();
        public Dictionary<string, List<Passenger>> Waiting { get; } = new Dictionary<string, List<Passenger>>();

        public int FreeSeats => SeatCount - Onboard.Count;
        public int SeatUpgradesLeft => SeatUpgradeStock - SeatUpgradesBought;
        public bool HasCarriage => CarriagesBought > 0;

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

        public void Grant(int points)
        {
            if (points > 0)
                Score += points;
        }

        public bool TryBuySeatUpgrade()
        {
            if (SeatUpgradesLeft <= 0 || Score < SeatUpgradeCost)
                return false;
            Score -= SeatUpgradeCost;
            SeatCount += SeatUpgradeSeats;
            SeatUpgradesBought++;
            return true;
        }

        public bool TryBuyCarriage()
        {
            if (HasCarriage || Score < CarriageCost)
                return false;
            Score -= CarriageCost;
            SeatCount += CarriageSeats;
            CarriagesBought++;
            return true;
        }

        public void Arrive(string townId)
        {
            CurrentTownId = townId;
        }

        public bool TryAlight(int passengerId, out bool scored)
        {
            scored = false;
            for (int i = 0; i < Onboard.Count; i++)
            {
                if (Onboard[i].Id != passengerId)
                    continue;
                var person = Onboard[i];
                Onboard.RemoveAt(i);
                if (person.DestId == CurrentTownId)
                {
                    Score += person.Fare;
                    scored = true;
                }
                else
                {
                    person.Relocate(CurrentTownId);
                    Waiting[CurrentTownId].Add(person);
                }
                return true;
            }

            return false;
        }

        public Passenger FindOnboard(int passengerId)
        {
            for (int i = 0; i < Onboard.Count; i++)
            {
                if (Onboard[i].Id == passengerId)
                    return Onboard[i];
            }

            return null;
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

        public List<Passenger> OnboardReadyFirst()
        {
            var ready = new List<Passenger>();
            var later = new List<Passenger>();
            for (int i = 0; i < Onboard.Count; i++)
            {
                if (Onboard[i].DestId == CurrentTownId)
                    ready.Add(Onboard[i]);
                else
                    later.Add(Onboard[i]);
            }

            for (int i = 0; i < later.Count; i++)
                ready.Add(later[i]);
            return ready;
        }

        public int CountOnboardTo(string destId)
        {
            int count = 0;
            for (int i = 0; i < Onboard.Count; i++)
            {
                if (Onboard[i].DestId == destId)
                    count++;
            }

            return count;
        }

        public List<DestinationTally> DestinationTallies()
        {
            var tallies = new List<DestinationTally>();
            for (int i = 0; i < RailroadGraph.Towns.Count; i++)
            {
                var town = RailroadGraph.Towns[i];
                int count = CountOnboardTo(town.Id);
                if (count == 0)
                    continue;
                tallies.Add(new DestinationTally(town.Id, town.Name, count));
            }

            for (int i = 0; i < tallies.Count; i++)
            {
                int best = i;
                for (int j = i + 1; j < tallies.Count; j++)
                {
                    int cmp = tallies[j].Count.CompareTo(tallies[best].Count);
                    if (cmp > 0 || (cmp == 0 && string.CompareOrdinal(tallies[j].TownName, tallies[best].TownName) < 0))
                        best = j;
                }

                if (best != i)
                {
                    var swap = tallies[i];
                    tallies[i] = tallies[best];
                    tallies[best] = swap;
                }
            }

            return tallies;
        }

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
