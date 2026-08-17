namespace Knightworld.Core
{
    public static class SimpleCombatAi
    {
        public static void TakeTurn(CombatSession session)
        {
            if (session.Outcome != CombatOutcome.Ongoing)
                return;
            var self = session.ActiveUnit;
            if (self == null || self.Team != Team.Enemy || self.IsDead)
                return;

            var enemies = session.LivingUnits(Team.Player);
            if (enemies.Count == 0)
                return;

            var inRange = ClosestInRange(session, self, enemies);
            if (inRange != null && self.HasAction)
            {
                session.TryAttack(inRange.Id);
                if (session.Outcome != CombatOutcome.Ongoing || session.ActiveUnit == null || session.ActiveUnit.Id != self.Id)
                    return;
            }

            if (self.RemainingMovementFeet >= GridMap.FeetPerSquare)
            {
                var closest = Closest(self, enemies);
                var dest = session.FindApproachCell(closest.Position);
                if (dest.HasValue && dest.Value != self.Position)
                {
                    session.TryMove(dest.Value);
                    if (session.Outcome != CombatOutcome.Ongoing || session.ActiveUnit == null || session.ActiveUnit.Id != self.Id || self.IsDead)
                        return;
                }
            }

            if (self.HasAction)
            {
                inRange = ClosestInRange(session, self, session.LivingUnits(Team.Player));
                if (inRange != null)
                {
                    session.TryAttack(inRange.Id);
                    if (session.Outcome != CombatOutcome.Ongoing || session.ActiveUnit == null || session.ActiveUnit.Id != self.Id)
                        return;
                }
            }

            if (session.Outcome == CombatOutcome.Ongoing && session.ActiveUnit != null && session.ActiveUnit.Id == self.Id)
                session.EndTurn();
        }

        private static UnitState ClosestInRange(CombatSession session, UnitState self, System.Collections.Generic.List<UnitState> enemies)
        {
            UnitState best = null;
            int bestDist = int.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!session.CanAttack(self, enemy))
                    continue;
                int dist = self.Position.Chebyshev(enemy.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }

            return best;
        }

        private static UnitState Closest(UnitState self, System.Collections.Generic.List<UnitState> enemies)
        {
            UnitState best = enemies[0];
            int bestDist = self.Position.Chebyshev(best.Position);
            for (int i = 1; i < enemies.Count; i++)
            {
                int dist = self.Position.Chebyshev(enemies[i].Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemies[i];
                }
            }

            return best;
        }
    }
}
