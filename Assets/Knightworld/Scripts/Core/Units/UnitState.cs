namespace Knightworld.Core
{
    public sealed class UnitState
    {
        public int Id { get; }
        public string Name { get; }
        public string ClassName { get; }
        public Team Team { get; }
        public GridPos Position { get; set; }
        public int MaxHp { get; }
        public int Hp { get; set; }
        public int ArmorClass { get; }
        public int SpeedFeet { get; }
        public int InitiativeBonus { get; }
        public int AttackBonus { get; }
        public int AttackRangeFeet { get; }
        public DiceFormula Damage { get; }
        public string AttackName { get; }

        public bool HasAction { get; set; }
        public bool HasBonusAction { get; set; }
        public bool HasReaction { get; set; }
        public int RemainingMovementFeet { get; set; }

        public bool IsDead => Hp <= 0;
        public int SpeedSquares => SpeedFeet / GridMap.FeetPerSquare;

        public UnitState(
            int id,
            string name,
            string className,
            Team team,
            GridPos position,
            int maxHp,
            int armorClass,
            int speedFeet,
            int initiativeBonus,
            int attackBonus,
            int attackRangeFeet,
            DiceFormula damage,
            string attackName)
        {
            Id = id;
            Name = name;
            ClassName = className;
            Team = team;
            Position = position;
            MaxHp = maxHp;
            Hp = maxHp;
            ArmorClass = armorClass;
            SpeedFeet = speedFeet;
            InitiativeBonus = initiativeBonus;
            AttackBonus = attackBonus;
            AttackRangeFeet = attackRangeFeet;
            Damage = damage;
            AttackName = attackName;
        }

        public void ResetTurnResources()
        {
            if (IsDead)
                return;
            HasAction = true;
            HasBonusAction = true;
            HasReaction = true;
            RemainingMovementFeet = SpeedFeet;
        }

        public void ApplyDamage(int amount)
        {
            if (amount < 0)
                amount = 0;
            Hp -= amount;
            if (Hp < 0)
                Hp = 0;
        }
    }
}
