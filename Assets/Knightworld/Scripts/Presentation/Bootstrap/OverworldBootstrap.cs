using Knightworld.Core;
using Knightworld.Presentation;
using UnityEngine;

namespace Knightworld.Bootstrap
{
    public sealed class OverworldBootstrap : MonoBehaviour
    {
        private void Start()
        {
            PlaceholderMaterials.Ensure();
            var root = new GameObject("Overworld").transform;
            var view = new OverworldView(root);
            view.Build();

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
            iso.Distance = 18f;
            iso.MinDistance = 10f;
            iso.MaxDistance = 32f;
            iso.Pitch = 52f;
            iso.FocusImmediate(view.Center);

            var hud = OverworldHud.Create();
            var controller = gameObject.AddComponent<OverworldController>();
            controller.Initialize(view, hud, camera);
        }
    }
}
