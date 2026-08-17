using System.Collections.Generic;
using Knightworld.Core;
using Knightworld.Data;
using Knightworld.Presentation;
using UnityEngine;

namespace Knightworld.Bootstrap
{
    public sealed class CombatBootstrap : MonoBehaviour
    {
        public EncounterDefinition encounter;
        public int randomSeed = 17;

        private void Start()
        {
            PlaceholderMaterials.Ensure();
            var spec = LevelCatalog.Get(CampaignState.PendingLevelId);
            int seed = string.IsNullOrEmpty(CampaignState.PendingLevelId) ? randomSeed : spec.Seed;
            var map = spec.CreateMap();
            var units = CreateUnits(spec);
            var session = new CombatSession(map, units, new SeededRandom(seed));

            var world = new GameObject("Battlefield").transform;
            new BattlefieldView(world).Build(map);
            var highlightRoot = new GameObject("Highlights").transform;
            highlightRoot.SetParent(world, false);
            var highlights = new GridHighlighter(highlightRoot);

            var unitRoot = new GameObject("Units").transform;
            var views = new List<UnitView>();
            foreach (var unit in units)
                views.Add(UnitView.Spawn(unit, unitRoot));

            var camera = Camera.main;
            if (camera == null)
            {
                var camGo = new GameObject("Main Camera");
                camera = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }

            var iso = camera.GetComponent<IsoCameraController>();
            if (iso == null)
                iso = camera.gameObject.AddComponent<IsoCameraController>();
            iso.Distance = 24f;
            iso.MaxDistance = 42f;
            iso.FocusImmediate(GridWorld.MapCenter(map));

            var hud = CombatHud.Create(session);
            var inputGo = new GameObject("CombatInput");
            var input = inputGo.AddComponent<CombatInput>();
            input.Session = session;
            input.Hud = hud;
            input.Highlights = highlights;
            input.WorldCamera = camera;

            var director = gameObject.AddComponent<CombatDirector>();
            director.Initialize(session, input, hud, iso, highlights, views);
            session.Start();
        }

        private List<UnitState> CreateUnits(LevelSpec spec)
        {
            if (!string.IsNullOrEmpty(CampaignState.PendingLevelId))
                return spec.CreateUnits();
            if (encounter == null)
                return spec.CreateUnits();
            return new List<UnitState>
            {
                encounter.CreateFighter(1, "Aldric", new GridPos(3, 3)),
                encounter.CreateWizard(2, "Seraphine", new GridPos(4, 3)),
                encounter.CreateGoblin(3, "Goblin Scout", new GridPos(19, 13)),
                encounter.CreateGoblin(4, "Goblin Cutthroat", new GridPos(20, 12)),
                encounter.CreateGoblin(5, "Goblin Archer", new GridPos(17, 14))
            };
        }
    }
}
