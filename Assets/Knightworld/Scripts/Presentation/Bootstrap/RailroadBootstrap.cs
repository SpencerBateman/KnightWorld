using Knightworld.Core;
using Knightworld.Presentation;
using UnityEngine;

namespace Knightworld.Bootstrap
{
    public sealed class RailroadBootstrap : MonoBehaviour
    {
        public TextAsset mapFile;
        public int randomSeed = 11;
        public int startingPassengersPerTown = 2;

        private void Start()
        {
            LoadMap();
            RailroadMaterials.Ensure();
            var session = new RailSession(new SeededRandom(randomSeed), RailroadGraph.StartTownId);
            session.SeedWaiting(startingPassengersPerTown);

            var root = new GameObject("Railroad").transform;
            var view = new RailroadView(root);
            view.Build();
            view.RefreshPassengers(session);

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
            iso.Distance = Mathf.Clamp(view.Radius * 1.55f + 10f, 16f, 64f);
            iso.MinDistance = 14f;
            iso.MaxDistance = iso.Distance + 28f;
            iso.Pitch = 52f;
            iso.FocusImmediate(view.Center);

            var hud = RailroadHud.Create();
            var controller = gameObject.AddComponent<RailroadController>();
            controller.Initialize(session, view, hud, camera);
        }

        private void LoadMap()
        {
            var asset = mapFile != null ? mapFile : Resources.Load<TextAsset>("Maps/the-local");
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                RailroadGraph.UseDefault();
                return;
            }

            try
            {
                RailroadGraph.Use(RailroadMapParser.Parse(asset.text));
            }
            catch (RailroadMapException ex)
            {
                Debug.LogError("Railroad map failed to load: " + ex.Message);
                RailroadGraph.UseDefault();
            }
        }
    }
}
