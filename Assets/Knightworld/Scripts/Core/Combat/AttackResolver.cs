namespace Knightworld.Core
{
    public sealed class AttackResult
    {
        public int AttackerId { get; set; }
        public int DefenderId { get; set; }
        public string AttackerName { get; set; }
        public string DefenderName { get; set; }
        public string AttackName { get; set; }
        public int D20 { get; set; }
        public int AttackBonus { get; set; }
        public int TotalToHit { get; set; }
        public int TargetAc { get; set; }
        public int CoverBonus { get; set; }
        public CoverLevel Cover { get; set; }
        public bool Hit { get; set; }
        public bool Critical { get; set; }
        public int Damage { get; set; }
        public bool Opportunity { get; set; }
        public bool DefenderDied { get; set; }

        public string LogLine
        {
            get
            {
                string coverText = Cover == CoverLevel.None ? "" : $" ({CoverRules.Label(Cover)})";
                string verb = Opportunity ? "opportunity attacks" : "attacks";
                if (Critical)
                    return $"{AttackerName} {verb} {DefenderName} with {AttackName}: 20 — Critical! {Damage} damage.";
                if (!Hit)
                    return $"{AttackerName} {verb} {DefenderName} with {AttackName}: {D20}+{AttackBonus}={TotalToHit} vs AC {TargetAc}{coverText} — Miss.";
                return $"{AttackerName} {verb} {DefenderName} with {AttackName}: {D20}+{AttackBonus}={TotalToHit} vs AC {TargetAc}{coverText} — Hit! {Damage} damage.";
            }
        }
    }

    public static class AttackResolver
    {
        public static AttackResult Resolve(UnitState attacker, UnitState defender, GridMap map, IRandom rng, bool opportunity = false)
        {
            var cover = map.GetCoverAgainst(attacker.Position, defender.Position);
            int coverBonus = CoverRules.ArmorBonus(cover);
            int d20 = rng.NextInclusive(1, 20);
            int total = d20 + attacker.AttackBonus;
            int targetAc = defender.ArmorClass + coverBonus;
            bool critical = d20 == 20;
            bool naturalMiss = d20 == 1;
            bool hit = !naturalMiss && (critical || total >= targetAc);
            int damage = 0;
            if (hit)
                damage = attacker.Damage.Roll(rng, critical);

            return new AttackResult
            {
                AttackerId = attacker.Id,
                DefenderId = defender.Id,
                AttackerName = attacker.Name,
                DefenderName = defender.Name,
                AttackName = attacker.AttackName,
                D20 = d20,
                AttackBonus = attacker.AttackBonus,
                TotalToHit = total,
                TargetAc = targetAc,
                CoverBonus = coverBonus,
                Cover = cover,
                Hit = hit,
                Critical = critical,
                Damage = damage,
                Opportunity = opportunity
            };
        }

        public static float EstimateHitChance(UnitState attacker, UnitState defender, GridMap map)
        {
            var cover = map.GetCoverAgainst(attacker.Position, defender.Position);
            if (cover == CoverLevel.Wall)
                return 0f;
            int coverBonus = CoverRules.ArmorBonus(cover);
            int needed = defender.ArmorClass + coverBonus - attacker.AttackBonus;
            int successes = 0;
            for (int roll = 1; roll <= 20; roll++)
            {
                bool hit = roll == 20 || (roll != 1 && roll >= needed);
                if (hit)
                    successes++;
            }

            return successes / 20f;
        }
    }
}
