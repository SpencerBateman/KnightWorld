using UnityEngine;

namespace Knightworld.Data
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Knightworld/Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string displayName = "Longsword";
        public int rangeFeet = 5;
        public int diceCount = 1;
        public int diceSides = 8;
        public int damageBonus;
        public int attackBonus = 5;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
