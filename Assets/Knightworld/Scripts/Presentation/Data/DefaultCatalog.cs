using Knightworld.Core;
using Knightworld.Data;
using UnityEngine;

namespace Knightworld.Bootstrap
{
    public static class DefaultCatalog
    {
        public static WeaponDefinition CreateLongsword()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.name = "Longsword";
            weapon.displayName = "Longsword";
            weapon.rangeFeet = CoreCatalog.Fighter.AttackRangeFeet;
            weapon.diceCount = CoreCatalog.Fighter.Damage.Count;
            weapon.diceSides = CoreCatalog.Fighter.Damage.Sides;
            weapon.damageBonus = CoreCatalog.Fighter.Damage.Bonus;
            weapon.attackBonus = CoreCatalog.Fighter.AttackBonus;
            return weapon;
        }

        public static WeaponDefinition CreateFireBolt()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.name = "Fire Bolt";
            weapon.displayName = "Fire Bolt";
            weapon.rangeFeet = CoreCatalog.Wizard.AttackRangeFeet;
            weapon.diceCount = CoreCatalog.Wizard.Damage.Count;
            weapon.diceSides = CoreCatalog.Wizard.Damage.Sides;
            weapon.damageBonus = CoreCatalog.Wizard.Damage.Bonus;
            weapon.attackBonus = CoreCatalog.Wizard.AttackBonus;
            return weapon;
        }

        public static WeaponDefinition CreateScimitar()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.name = "Scimitar";
            weapon.displayName = "Scimitar";
            weapon.rangeFeet = CoreCatalog.Goblin.AttackRangeFeet;
            weapon.diceCount = CoreCatalog.Goblin.Damage.Count;
            weapon.diceSides = CoreCatalog.Goblin.Damage.Sides;
            weapon.damageBonus = CoreCatalog.Goblin.Damage.Bonus;
            weapon.attackBonus = CoreCatalog.Goblin.AttackBonus;
            return weapon;
        }

        public static ClassDefinition CreateFighter(WeaponDefinition longsword)
        {
            var def = ScriptableObject.CreateInstance<ClassDefinition>();
            def.name = "Fighter";
            def.displayName = CoreCatalog.Fighter.ClassName;
            def.maxHp = CoreCatalog.Fighter.MaxHp;
            def.armorClass = CoreCatalog.Fighter.ArmorClass;
            def.speedFeet = CoreCatalog.Fighter.SpeedFeet;
            def.initiativeBonus = CoreCatalog.Fighter.InitiativeBonus;
            def.weapon = longsword;
            return def;
        }

        public static ClassDefinition CreateWizard(WeaponDefinition fireBolt)
        {
            var def = ScriptableObject.CreateInstance<ClassDefinition>();
            def.name = "Wizard";
            def.displayName = CoreCatalog.Wizard.ClassName;
            def.maxHp = CoreCatalog.Wizard.MaxHp;
            def.armorClass = CoreCatalog.Wizard.ArmorClass;
            def.speedFeet = CoreCatalog.Wizard.SpeedFeet;
            def.initiativeBonus = CoreCatalog.Wizard.InitiativeBonus;
            def.weapon = fireBolt;
            return def;
        }

        public static ClassDefinition CreateGoblin(WeaponDefinition scimitar)
        {
            var def = ScriptableObject.CreateInstance<ClassDefinition>();
            def.name = "Goblin";
            def.displayName = CoreCatalog.Goblin.ClassName;
            def.maxHp = CoreCatalog.Goblin.MaxHp;
            def.armorClass = CoreCatalog.Goblin.ArmorClass;
            def.speedFeet = CoreCatalog.Goblin.SpeedFeet;
            def.initiativeBonus = CoreCatalog.Goblin.InitiativeBonus;
            def.weapon = scimitar;
            return def;
        }
    }
}
