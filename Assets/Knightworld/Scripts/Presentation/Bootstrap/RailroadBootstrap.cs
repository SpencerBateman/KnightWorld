using Knightworld.Core;
using Knightworld.Presentation;
using UnityEngine;

namespace Knightworld.Bootstrap
{
    public sealed class RailroadBootstrap : MonoBehaviour
    {
        public int randomSeed = 11;
        public int startingPassengersPerTown = 2;

        private void Start()
        {
            RailroadMaterials.Ensure();
            var session = new RailSession(new SeededRandom(randomSeed), RailroadGraph.Millhaven);
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
            iso.Distance = 22f;
            iso.MinDistance = 12f;
            iso.MaxDistance = 36f;
            iso.Pitch = 52f;
            iso.FocusImmediate(view.Center);

            var hud = RailroadHud.Create();
            var controller = gameObject.AddComponent<RailroadController>();
            controller.Initialize(session, view, hud, camera);
        }
    }
}
