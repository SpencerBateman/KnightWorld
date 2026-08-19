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
            RailSession session;
            if (!RailSaveStore.TryLoad(out session))
            {
                session = new RailSession(new SeededRandom(randomSeed), RailroadGraph.StartTownId);
                session.SeedWaiting(startingPassengersPerTown);
            }

            var root = new GameObject("Railroad").transform;
            var view = new RailroadView(root);
            view.Build();
            RevealUnlocked(view, session);
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
            iso.Distance = 11f;
            iso.MinDistance = 6f;
            iso.MaxDistance = 32f;
            iso.Pitch = 48f;
            iso.FollowLerp = 10f;
            iso.IgnorePan = true;
            iso.FocusImmediate(view.Train.position);
            view.ApplyCamera(camera);

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

        private static void RevealUnlocked(RailroadView view, RailSession session)
        {
            foreach (var key in session.UnlockedKeys)
            {
                if (!RailroadMap.SplitTrackKey(key, out string a, out string b))
                    continue;
                view.UnlockTrack(a, b);
            }
        }
    }
}
