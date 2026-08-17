namespace Knightworld.Core
{
    public sealed class UnitTemplate
    {
        public string ClassName { get; set; }
        public int MaxHp { get; set; }
        public int ArmorClass { get; set; }
        public int SpeedFeet { get; set; }
        public int InitiativeBonus { get; set; }
        public int AttackBonus { get; set; }
        public int AttackRangeFeet { get; set; }
        public DiceFormula Damage { get; set; }
        public string AttackName { get; set; }

        public UnitState Instantiate(int id, string name, Team team, GridPos position)
        {
            return new UnitState(
                id,
                name,
                ClassName,
                team,
                position,
                MaxHp,
                ArmorClass,
                SpeedFeet,
                InitiativeBonus,
                AttackBonus,
                AttackRangeFeet,
                Damage,
                AttackName);
        }
    }

    public static class CoreCatalog
    {
        public static UnitTemplate Fighter { get; } = new UnitTemplate
        {
            ClassName = "Fighter",
            MaxHp = 12,
            ArmorClass = 16,
            SpeedFeet = 30,
            InitiativeBonus = 2,
            AttackBonus = 5,
            AttackRangeFeet = 5,
            Damage = new DiceFormula(1, 8, 3),
            AttackName = "Longsword"
        };

        public static UnitTemplate Wizard { get; } = new UnitTemplate
        {
            ClassName = "Wizard",
            MaxHp = 8,
            ArmorClass = 12,
            SpeedFeet = 30,
            InitiativeBonus = 2,
            AttackBonus = 5,
            AttackRangeFeet = 120,
            Damage = new DiceFormula(1, 10, 0),
            AttackName = "Fire Bolt"
        };

        public static UnitTemplate Goblin { get; } = new UnitTemplate
        {
            ClassName = "Goblin",
            MaxHp = 7,
            ArmorClass = 15,
            SpeedFeet = 30,
            InitiativeBonus = 2,
            AttackBonus = 4,
            AttackRangeFeet = 5,
            Damage = new DiceFormula(1, 6, 2),
            AttackName = "Scimitar"
        };
    }
}
