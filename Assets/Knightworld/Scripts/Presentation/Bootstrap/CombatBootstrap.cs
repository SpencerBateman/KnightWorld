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
            var map = TestDungeon.CreateMap();
            var units = CreateUnits();
            var session = new CombatSession(map, units, new SeededRandom(randomSeed));

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

        private List<UnitState> CreateUnits()
        {
            if (encounter == null)
                return TestDungeon.CreateUnits();
            return new List<UnitState>
            {
                encounter.CreateFighter(1, "Aldric", new GridPos(2, 2)),
                encounter.CreateWizard(2, "Seraphine", new GridPos(3, 2)),
                encounter.CreateGoblin(3, "Goblin Scout", new GridPos(8, 7)),
                encounter.CreateGoblin(4, "Goblin Cutthroat", new GridPos(9, 6)),
                encounter.CreateGoblin(5, "Goblin Archer", new GridPos(6, 8))
            };
        }
    }
}
