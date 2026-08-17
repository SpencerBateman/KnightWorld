using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Data
{
    [CreateAssetMenu(fileName = "Class", menuName = "Knightworld/Class")]
    public sealed class ClassDefinition : ScriptableObject
    {
        public string displayName = "Fighter";
        public int maxHp = 12;
        public int armorClass = 16;
        public int speedFeet = 30;
        public int initiativeBonus = 2;
        public WeaponDefinition weapon;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public UnitTemplate ToTemplate()
        {
            var weaponDef = weapon;
            return new UnitTemplate
            {
                ClassName = DisplayName,
                MaxHp = maxHp,
                ArmorClass = armorClass,
                SpeedFeet = speedFeet,
                InitiativeBonus = initiativeBonus,
                AttackBonus = weaponDef != null ? weaponDef.attackBonus : 4,
                AttackRangeFeet = weaponDef != null ? weaponDef.rangeFeet : 5,
                Damage = weaponDef != null
                    ? new DiceFormula(weaponDef.diceCount, weaponDef.diceSides, weaponDef.damageBonus)
                    : new DiceFormula(1, 6, 0),
                AttackName = weaponDef != null ? weaponDef.DisplayName : "Unarmed Strike"
            };
        }
    }
}
