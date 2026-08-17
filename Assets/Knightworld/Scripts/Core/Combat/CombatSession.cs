using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public sealed class CombatSession
    {
        private readonly List<UnitState> _units;
        private readonly IRandom _rng;

        public GridMap Map { get; }
        public InitiativeQueue Initiative { get; }
        public CombatOutcome Outcome { get; private set; }
        public IReadOnlyList<UnitState> Units => _units;

        public event Action<UnitState> TurnStarted;
        public event Action<UnitState, IReadOnlyList<GridPos>> UnitMoved;
        public event Action<AttackResult> AttackResolved;
        public event Action<UnitState> UnitDied;
        public event Action<CombatOutcome> CombatEnded;
        public event Action<string> LogGenerated;

        public CombatSession(GridMap map, IReadOnlyList<UnitState> units, IRandom rng)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _units = new List<UnitState>(units);
            Initiative = new InitiativeQueue();
            Outcome = CombatOutcome.Ongoing;
        }

        public UnitState ActiveUnit => GetUnit(Initiative.CurrentUnitId);

        public UnitState GetUnit(int id)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Id == id)
                    return _units[i];
            }

            return null;
        }

        public UnitState UnitAt(GridPos pos)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (!_units[i].IsDead && _units[i].Position == pos)
                    return _units[i];
            }

            return null;
        }

        public List<UnitState> LivingUnits(Team team)
        {
            var list = new List<UnitState>();
            for (int i = 0; i < _units.Count; i++)
            {
                if (!_units[i].IsDead && _units[i].Team == team)
                    list.Add(_units[i]);
            }

            return list;
        }

        public void Start()
        {
            foreach (var unit in _units)
                unit.ResetTurnResources();
            Initiative.Roll(_units, _rng);
            var first = ActiveUnit;
            Log($"Combat begins. Round {Initiative.Round}.");
            if (first != null)
                Log($"{first.Name} wins initiative ({GetInitiativeRoll(first.Id)}).");
            TurnStarted?.Invoke(first);
        }

        public HashSet<GridPos> GetReachableCells()
        {
            var mover = ActiveUnit;
            if (mover == null || mover.IsDead)
                return new HashSet<GridPos>();
            int squares = mover.RemainingMovementFeet / GridMap.FeetPerSquare;
            return Pathfinder.FindReachable(
                Map,
                mover.Position,
                squares,
                pos => BlocksPass(mover, pos),
                BlocksStand);
        }

        public List<GridPos> GetPathTo(GridPos dest)
        {
            var mover = ActiveUnit;
            if (mover == null)
                return null;
            return Pathfinder.FindPath(
                Map,
                mover.Position,
                dest,
                pos => BlocksPass(mover, pos),
                BlocksStand);
        }

        public bool CanAttack(UnitState attacker, UnitState defender)
        {
            if (attacker == null || defender == null || attacker.IsDead || defender.IsDead)
                return false;
            if (attacker.Team == defender.Team)
                return false;
            if (!attacker.HasAction && attacker == ActiveUnit)
                return false;
            if (attacker.Position.DistanceFeet(defender.Position) > attacker.AttackRangeFeet)
                return false;
            var cover = Map.GetCoverAgainst(attacker.Position, defender.Position);
            return cover != CoverLevel.Wall;
        }

        public bool TryGetHitChance(int targetId, out float chance)
        {
            chance = 0f;
            var attacker = ActiveUnit;
            var defender = GetUnit(targetId);
            if (!CanAttack(attacker, defender))
                return false;
            chance = AttackResolver.EstimateHitChance(attacker, defender, Map);
            return true;
        }

        public MoveResult TryMove(GridPos dest)
        {
            var result = new MoveResult();
            var mover = ActiveUnit;
            if (Outcome != CombatOutcome.Ongoing)
            {
                result.FailReason = "Combat is over.";
                return result;
            }

            if (mover == null || mover.IsDead)
            {
                result.FailReason = "No active unit.";
                return result;
            }

            if (dest == mover.Position)
            {
                result.FailReason = "Already there.";
                return result;
            }

            var path = GetPathTo(dest);
            if (path == null || path.Count < 2)
            {
                result.FailReason = "No path.";
                return result;
            }

            int squares = path.Count - 1;
            int cost = squares * GridMap.FeetPerSquare;
            if (cost > mover.RemainingMovementFeet)
            {
                result.FailReason = "Not enough movement.";
                return result;
            }

            result.Success = true;
            result.PathTaken.Add(mover.Position);
            for (int i = 1; i < path.Count; i++)
            {
                var from = mover.Position;
                var to = path[i];
                ResolveOpportunityAttacks(mover, from, to, result);
                if (mover.IsDead)
                {
                    result.MoverDied = true;
                    break;
                }

                mover.Position = to;
                mover.RemainingMovementFeet -= GridMap.FeetPerSquare;
                result.PathTaken.Add(to);
            }

            UnitMoved?.Invoke(mover, result.PathTaken);
            RefreshOutcome();
            return result;
        }

        public AttackResult TryAttack(int targetId)
        {
            if (Outcome != CombatOutcome.Ongoing)
                return null;
            var attacker = ActiveUnit;
            var defender = GetUnit(targetId);
            if (!CanAttack(attacker, defender) || !attacker.HasAction)
                return null;

            var result = AttackResolver.Resolve(attacker, defender, Map, _rng);
            attacker.HasAction = false;
            ApplyAttack(result, defender);
            return result;
        }

        public EndTurnResult EndTurn()
        {
            var result = new EndTurnResult();
            if (Outcome != CombatOutcome.Ongoing)
                return result;
            var previous = ActiveUnit;
            result.PreviousUnitId = previous != null ? previous.Id : 0;
            if (!Initiative.AdvanceToNextLiving(_units))
            {
                RefreshOutcome();
                return result;
            }

            var next = ActiveUnit;
            next?.ResetTurnResources();
            result.NextUnitId = next != null ? next.Id : 0;
            result.Round = Initiative.Round;
            if (next != null)
                Log($"{next.Name}'s turn (round {Initiative.Round}).");
            TurnStarted?.Invoke(next);
            return result;
        }

        public GridPos? FindApproachCell(GridPos target)
        {
            var reachable = GetReachableCells();
            GridPos? best = null;
            int bestDist = int.MaxValue;
            foreach (var pos in reachable)
            {
                int dist = pos.Chebyshev(target);
                if (dist < bestDist || (dist == bestDist && best != null && (pos.Y < best.Value.Y || (pos.Y == best.Value.Y && pos.X < best.Value.X))))
                {
                    bestDist = dist;
                    best = pos;
                }
            }

            return best;
        }

        private void ResolveOpportunityAttacks(UnitState mover, GridPos from, GridPos to, MoveResult move)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var enemy = _units[i];
                if (enemy.IsDead || enemy.Team == mover.Team || !enemy.HasReaction)
                    continue;
                if (from.DistanceFeet(enemy.Position) > enemy.AttackRangeFeet)
                    continue;
                if (to.DistanceFeet(enemy.Position) <= enemy.AttackRangeFeet)
                    continue;
                if (Map.GetCoverAgainst(enemy.Position, from) == CoverLevel.Wall)
                    continue;

                enemy.HasReaction = false;
                var attack = AttackResolver.Resolve(enemy, mover, Map, _rng, true);
                ApplyAttack(attack, mover);
                move.OpportunityAttacks.Add(attack);
                if (mover.IsDead)
                    return;
            }
        }

        private void ApplyAttack(AttackResult result, UnitState defender)
        {
            if (result.Hit)
            {
                defender.ApplyDamage(result.Damage);
                if (defender.IsDead)
                {
                    result.DefenderDied = true;
                    Log(result.LogLine + $" {defender.Name} falls.");
                    UnitDied?.Invoke(defender);
                }
                else
                {
                    Log(result.LogLine);
                }
            }
            else
            {
                Log(result.LogLine);
            }

            AttackResolved?.Invoke(result);
            RefreshOutcome();
        }

        private bool BlocksPass(UnitState mover, GridPos pos)
        {
            var occupant = UnitAt(pos);
            if (occupant == null)
                return false;
            return occupant.Team != mover.Team;
        }

        private bool BlocksStand(GridPos pos)
        {
            return UnitAt(pos) != null;
        }

        private void RefreshOutcome()
        {
            if (Outcome != CombatOutcome.Ongoing)
                return;
            bool playersAlive = LivingUnits(Team.Player).Count > 0;
            bool enemiesAlive = LivingUnits(Team.Enemy).Count > 0;
            if (playersAlive && enemiesAlive)
                return;
            Outcome = playersAlive ? CombatOutcome.PlayerVictory : CombatOutcome.PlayerDefeat;
            Log(Outcome == CombatOutcome.PlayerVictory ? "Victory! The dungeon is cleared." : "Defeat. The knights have fallen.");
            CombatEnded?.Invoke(Outcome);
        }

        private string GetInitiativeRoll(int unitId)
        {
            foreach (var entry in Initiative.Order)
            {
                if (entry.UnitId == unitId)
                    return $"{entry.Roll}+{entry.Bonus}={entry.Total}";
            }

            return "?";
        }

        private void Log(string line) => LogGenerated?.Invoke(line);
    }
}
