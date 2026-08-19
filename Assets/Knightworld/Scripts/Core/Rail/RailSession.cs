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
            : this(id, name, originId, destId, FareBetween(originId, destId))
        {
        }

        public Passenger(int id, string name, string originId, string destId, int fare)
        {
            Id = id;
            Name = name;
            OriginId = originId;
            DestId = destId;
            Fare = fare < 1 ? 1 : fare;
        }

        public void Relocate(string townId)
        {
            OriginId = townId;
            Fare = FareBetween(townId, DestId);
        }

        public void Relocate(string townId, int fare)
        {
            OriginId = townId;
            Fare = fare < 1 ? 1 : fare;
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
        public const int MoveSpawnChancePercent = 40;
        public static readonly string[] Names =
        {
            "Ada", "Bram", "Cora", "Dax", "Elia", "Flint", "Gita", "Holt", "Ines", "Joss",
            "Kade", "Lina", "Moss", "Nyla", "Otto", "Pia", "Quinn", "Rusk", "Sable", "Tess"
        };

        private readonly IRandom _random;
        private readonly SeededRandom _seeded;
        private int _nextPassengerId = 1;

        public int Score { get; private set; }
        public int SeatCount { get; private set; } = StartingSeats;
        public int SeatUpgradesBought { get; private set; }
        public int CarriagesBought { get; private set; }
        public string CurrentTownId { get; private set; }
        public string TravelFromId { get; private set; }
        public string TravelToId { get; private set; }
        public long TravelDepartUtcTicks { get; private set; }
        public float TravelDurationSeconds { get; private set; }
        public List<Passenger> Onboard { get; } = new List<Passenger>();
        public Dictionary<string, List<Passenger>> Waiting { get; } = new Dictionary<string, List<Passenger>>();
        private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);

        public int FreeSeats => SeatCount - Onboard.Count;
        public int SeatUpgradesLeft => SeatUpgradeStock - SeatUpgradesBought;
        public bool HasCarriage => CarriagesBought > 0;
        public bool InTransit => !string.IsNullOrEmpty(TravelToId);
        public IReadOnlyCollection<string> UnlockedKeys => _unlocked;

        public RailSession(IRandom random, string startTownId)
        {
            _random = random ?? new SeededRandom(11);
            _seeded = _random as SeededRandom;
            CurrentTownId = startTownId;
            for (int i = 0; i < RailroadGraph.Towns.Count; i++)
                Waiting[RailroadGraph.Towns[i].Id] = new List<Passenger>();
        }

        public void SeedWaiting(int perTown)
        {
            for (int t = 0; t < RailroadGraph.Towns.Count; t++)
            {
                if (!IsAccessible(RailroadGraph.Towns[t].Id))
                    continue;
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
                if (!IsAccessible(id) || Waiting[id].Count >= MaxWaitingPerTown)
                    continue;
                open.Add(id);
            }

            if (open.Count == 0)
                return false;
            return TrySpawnAt(open[_random.NextInclusive(0, open.Count - 1)]);
        }

        public int RollPassengersOnMove()
        {
            int spawned = 0;
            for (int i = 0; i < RailroadGraph.Towns.Count; i++)
            {
                string id = RailroadGraph.Towns[i].Id;
                if (id == CurrentTownId || !IsAccessible(id))
                    continue;
                if (_random.NextInclusive(1, 100) > MoveSpawnChancePercent)
                    continue;
                if (TrySpawnAt(id))
                    spawned++;
            }

            return spawned;
        }

        public bool TrySpawnAt(string townId)
        {
            if (!Waiting.ContainsKey(townId) || Waiting[townId].Count >= MaxWaitingPerTown)
                return false;
            if (!IsAccessible(townId))
                return false;
            string dest = RandomAccessibleTown(townId);
            if (dest == null)
                return false;
            string name = Names[_random.NextInclusive(0, Names.Length - 1)];
            Waiting[townId].Add(new Passenger(_nextPassengerId++, name, townId, dest, OpenFare(townId, dest)));
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

        public bool CanRide(string fromId, string toId)
        {
            if (!RailroadGraph.AreLinked(fromId, toId))
                return false;
            if (!RailroadGraph.IsLocked(fromId, toId))
                return true;
            return _unlocked.Contains(RailroadMap.TrackKey(fromId, toId));
        }

        public bool RouteOwned(string fromId, string toId)
        {
            return _unlocked.Contains(RailroadMap.TrackKey(fromId, toId));
        }

        public bool TryBuyRoute(string otherTownId)
        {
            var locked = RailroadGraph.LockedTrack(CurrentTownId, otherTownId);
            if (locked == null)
                return false;
            string key = RailroadMap.TrackKey(locked.A, locked.B);
            if (_unlocked.Contains(key) || Score < locked.Cost)
                return false;
            Score -= locked.Cost;
            _unlocked.Add(key);
            return true;
        }

        public void Arrive(string townId)
        {
            CurrentTownId = townId;
            ClearTrip();
        }

        public bool TryDepart(string toId, DateTime utcNow)
        {
            if (InTransit || string.IsNullOrEmpty(toId) || !CanRide(CurrentTownId, toId))
                return false;
            RollPassengersOnMove();
            TravelFromId = CurrentTownId;
            TravelToId = toId;
            TravelDepartUtcTicks = utcNow.Kind == DateTimeKind.Utc ? utcNow.Ticks : utcNow.ToUniversalTime().Ticks;
            TravelDurationSeconds = RailroadGraph.TravelSeconds(RailroadGraph.Distance(CurrentTownId, toId));
            return true;
        }

        public float TravelElapsedSeconds(DateTime utcNow)
        {
            if (!InTransit)
                return 0f;
            long now = utcNow.Kind == DateTimeKind.Utc ? utcNow.Ticks : utcNow.ToUniversalTime().Ticks;
            double elapsed = (now - TravelDepartUtcTicks) / (double)TimeSpan.TicksPerSecond;
            if (elapsed < 0d)
                return 0f;
            return (float)elapsed;
        }

        public float TravelRemainingSeconds(DateTime utcNow)
        {
            if (!InTransit)
                return 0f;
            float left = TravelDurationSeconds - TravelElapsedSeconds(utcNow);
            return left < 0f ? 0f : left;
        }

        public float TravelProgress(DateTime utcNow)
        {
            if (!InTransit)
                return 0f;
            if (TravelDurationSeconds <= 0.001f)
                return 1f;
            float t = TravelElapsedSeconds(utcNow) / TravelDurationSeconds;
            if (t < 0f)
                return 0f;
            if (t > 1f)
                return 1f;
            return t;
        }

        public bool FinishTravelIfDue(DateTime utcNow)
        {
            if (!InTransit)
                return false;
            if (TravelRemainingSeconds(utcNow) > 0f)
                return false;
            string dest = TravelToId;
            Arrive(dest);
            return true;
        }

        private void ClearTrip()
        {
            TravelFromId = null;
            TravelToId = null;
            TravelDepartUtcTicks = 0L;
            TravelDurationSeconds = 0f;
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
                    person.Relocate(CurrentTownId, OpenFare(CurrentTownId, person.DestId));
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

        public bool IsAccessible(string townId)
        {
            if (string.IsNullOrEmpty(townId))
                return false;
            var reachable = AccessibleTowns();
            for (int i = 0; i < reachable.Count; i++)
            {
                if (reachable[i] == townId)
                    return true;
            }

            return false;
        }

        public List<string> AccessibleTowns()
        {
            var found = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string start = RailroadGraph.StartTownId;
            if (string.IsNullOrEmpty(start))
                start = CurrentTownId;
            seen.Add(start);
            found.Add(start);
            for (int i = 0; i < found.Count; i++)
            {
                var town = RailroadGraph.Get(found[i]);
                for (int n = 0; n < town.Links.Count; n++)
                {
                    string next = town.Links[n];
                    if (seen.Contains(next) || !CanRide(found[i], next))
                        continue;
                    seen.Add(next);
                    found.Add(next);
                }
            }

            return found;
        }

        private int OpenFare(string fromId, string toId)
        {
            if (fromId == toId)
                return 1;
            var route = RailroadGraph.FindRoute(fromId, toId, CanRide);
            if (route == null)
                return 1;
            int fare = (int)Math.Round(RailroadGraph.RouteDistance(route));
            return fare < 1 ? 1 : fare;
        }

        private string RandomAccessibleTown(string originId)
        {
            var choices = new List<string>();
            var reachable = AccessibleTowns();
            for (int i = 0; i < reachable.Count; i++)
            {
                if (reachable[i] != originId)
                    choices.Add(reachable[i]);
            }

            if (choices.Count == 0)
                return null;
            return choices[_random.NextInclusive(0, choices.Count - 1)];
        }

        public RailSaveState Capture()
        {
            var state = new RailSaveState
            {
                MapTitle = RailroadGraph.Map != null ? RailroadGraph.Map.Title : "",
                StartTownId = RailroadGraph.StartTownId,
                SeedState = _seeded != null ? _seeded.State : 0,
                Score = Score,
                SeatCount = SeatCount,
                SeatUpgradesBought = SeatUpgradesBought,
                CarriagesBought = CarriagesBought,
                NextPassengerId = _nextPassengerId,
                CurrentTownId = CurrentTownId,
                TravelFromId = TravelFromId ?? "",
                TravelToId = TravelToId ?? "",
                TravelDepartUtcTicks = TravelDepartUtcTicks,
                TravelDurationSeconds = TravelDurationSeconds
            };
            foreach (var key in _unlocked)
                state.Unlocked.Add(key);
            for (int i = 0; i < Onboard.Count; i++)
                state.Onboard.Add(RailSaveState.FromPassenger(Onboard[i]));
            foreach (var pair in Waiting)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                    state.Waiting.Add(RailSaveState.FromPassenger(pair.Value[i]));
            }

            return state;
        }

        public static RailSession FromSave(RailSaveState state)
        {
            var rng = new SeededRandom(state != null && state.SeedState != 0 ? state.SeedState : 11);
            if (state != null && state.SeedState != 0)
                rng.Restore(state.SeedState);
            string town = state != null && !string.IsNullOrEmpty(state.CurrentTownId)
                ? state.CurrentTownId
                : RailroadGraph.StartTownId;
            var session = new RailSession(rng, town);
            if (state == null)
                return session;
            session.Score = state.Score;
            session.SeatCount = state.SeatCount < StartingSeats ? StartingSeats : state.SeatCount;
            session.SeatUpgradesBought = state.SeatUpgradesBought;
            session.CarriagesBought = state.CarriagesBought;
            session._nextPassengerId = state.NextPassengerId < 1 ? 1 : state.NextPassengerId;
            for (int i = 0; i < state.Unlocked.Count; i++)
            {
                if (!string.IsNullOrEmpty(state.Unlocked[i]))
                    session._unlocked.Add(state.Unlocked[i]);
            }

            for (int i = 0; i < state.Onboard.Count; i++)
            {
                var person = state.Onboard[i].ToPassenger();
                if (person != null)
                    session.Onboard.Add(person);
            }

            for (int i = 0; i < state.Waiting.Count; i++)
            {
                var person = state.Waiting[i].ToPassenger();
                if (person == null || !session.Waiting.ContainsKey(person.OriginId))
                    continue;
                session.Waiting[person.OriginId].Add(person);
            }

            if (!string.IsNullOrEmpty(state.TravelToId))
            {
                session.TravelFromId = string.IsNullOrEmpty(state.TravelFromId) ? town : state.TravelFromId;
                session.TravelToId = state.TravelToId;
                session.TravelDepartUtcTicks = state.TravelDepartUtcTicks;
                session.TravelDurationSeconds = state.TravelDurationSeconds;
            }

            return session;
        }
    }
}
