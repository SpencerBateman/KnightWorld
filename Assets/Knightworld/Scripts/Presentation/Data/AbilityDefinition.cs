using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Data
{
    [CreateAssetMenu(fileName = "Ability", menuName = "Knightworld/Ability")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        public string displayName = "Attack";
        [TextArea] public string description = "Make a weapon or cantrip attack.";
        public bool usesAction = true;
        public bool usesBonusAction;
    }
}
