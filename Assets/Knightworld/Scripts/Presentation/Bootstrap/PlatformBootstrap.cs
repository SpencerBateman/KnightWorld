using Knightworld.Core;
using Knightworld.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Knightworld.Bootstrap
{
    public sealed class PlatformBootstrap : MonoBehaviour
    {
        public TextAsset mapFile;
        public int randomSeed = 11;
        public int startingPassengersPerTown = 2;
        public int npcCount = 8;

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

            session.EnsureQuestPassenger();

            var root = new GameObject("Platform").transform;
            var view = new PlatformView(root);
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
            if (iso != null)
                Destroy(iso);

            view.ApplyCamera(camera);
            CreateHint();

            var hud = RailroadHud.Create();
            var desk = gameObject.AddComponent<PlatformStationDesk>();
            desk.Initialize(session, hud, view.StationDeskZone);

            var train = view.TrainRoot.gameObject.AddComponent<PlatformTrain>();
            train.Initialize(view.BoardingZone);

            var player = PlatformFigure.SpawnPlayer(root, view.SpawnPoint);
            var walker = player.AddComponent<PlatformPlayer>();
            walker.Initialize(camera, view.WalkBounds, train, desk);

            SpawnCrowd(root, view);

            var diablo = camera.GetComponent<DiabloCameraController>();
            if (diablo == null)
                diablo = camera.gameObject.AddComponent<DiabloCameraController>();
            diablo.Distance = 12f;
            diablo.MinDistance = 6f;
            diablo.MaxDistance = 22f;
            diablo.Pitch = 62f;
            diablo.Yaw = 0f;
            diablo.FollowLerp = 14f;
            diablo.SnapTo(player.transform);
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

        private void SpawnCrowd(Transform parent, PlatformView view)
        {
            var crowd = new GameObject("Crowd").transform;
            crowd.SetParent(parent, false);
            float minZ = -PlatformView.WalkHalfLength + 1f;
            float maxZ = PlatformView.WalkHalfLength - 1f;
            int count = Mathf.Max(2, npcCount);

            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                float z = Mathf.Lerp(minZ, maxZ, t);
                float lane = ((i % 4) - 1.5f) * 0.95f;
                lane = Mathf.Clamp(lane, -PlatformView.WalkHalfWidth + 0.4f, PlatformView.WalkHalfWidth - 0.4f);
                bool goingUp = i % 2 == 0;
                float direction = goingUp ? 1f : -1f;
                float speed = 2.2f + (i % 5) * 0.35f;

                var npc = PlatformFigure.SpawnNpc(crowd, new Vector3(lane, view.WalkY, z), i);
                var mover = npc.AddComponent<PlatformNpc>();
                mover.Initialize(lane, view.WalkY, minZ, maxZ, direction, speed, true);
            }
        }

        private static void CreateHint()
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("PlatformHud", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("Hint", typeof(RectTransform));
            var rect = textGo.GetComponent<RectTransform>();
            rect.SetParent(canvasGo.transform, false);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 28f);
            rect.sizeDelta = new Vector2(1100f, 60f);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.LowerCenter;
            text.color = Color.white;
            text.text = "Blue pad: station desk. Yellow pad: board the train.";
            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }
}
