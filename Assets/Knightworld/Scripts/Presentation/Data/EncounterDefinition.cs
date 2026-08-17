using Knightworld.Core;
using Knightworld.Data;
using UnityEngine;

namespace Knightworld.Data
{
    [CreateAssetMenu(fileName = "Encounter", menuName = "Knightworld/Encounter")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        public ClassDefinition fighterClass;
        public ClassDefinition wizardClass;
        public ClassDefinition goblinClass;

        public UnitState CreateFighter(int id, string name, GridPos position)
        {
            var template = fighterClass != null ? fighterClass.ToTemplate() : CoreCatalog.Fighter;
            return template.Instantiate(id, name, Team.Player, position);
        }

        public UnitState CreateWizard(int id, string name, GridPos position)
        {
            var template = wizardClass != null ? wizardClass.ToTemplate() : CoreCatalog.Wizard;
            return template.Instantiate(id, name, Team.Player, position);
        }

        public UnitState CreateGoblin(int id, string name, GridPos position)
        {
            var template = goblinClass != null ? goblinClass.ToTemplate() : CoreCatalog.Goblin;
            return template.Instantiate(id, name, Team.Enemy, position);
        }
    }
}
