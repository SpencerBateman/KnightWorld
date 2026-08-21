using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class PlatformView
    {
        public const float Length = 24f;
        public const float Width = 6.5f;
        public const float DeckY = 0.35f;
        public const float WalkHalfWidth = 2.85f;
        public const float WalkHalfLength = 11.25f;
        public const float RailX = -Width * 0.5f - 2.2f;

        private readonly Transform _root;
        private Transform _trainRoot;
        private Transform _boardSeat;
        private Bounds _boardingZone;
        private Bounds _stationDeskZone;

        public PlatformView(Transform root)
        {
            _root = root;
        }

        public Vector3 SpawnPoint => new Vector3(0f, WalkY, -WalkHalfLength + 2f);

        public float WalkY => DeckY + 0.05f;
        public Transform TrainRoot => _trainRoot;
        public Transform BoardSeat => _boardSeat;
        public Bounds BoardingZone => _boardingZone;
        public Bounds StationDeskZone => _stationDeskZone;

        public Bounds WalkBounds
        {
            get
            {
                var center = new Vector3(0f, WalkY, 0f);
                var size = new Vector3(WalkHalfWidth * 2f, 0.1f, WalkHalfLength * 2f);
                return new Bounds(center, size);
            }
        }

        public void Build()
        {
            RailroadMaterials.Ensure();
            BuildGround();
            BuildDeck();
            BuildRails();
            BuildTrain();
            BuildFurniture();
            BuildStationHouse();
            MeshBaker.Bake(_root);
        }

        public void ApplyCamera(Camera camera)
        {
            if (camera == null)
                return;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
            camera.farClipPlane = 120f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.68f, 0.78f, 0.88f);
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 70f;
            RenderSettings.ambientLight = new Color(0.76f, 0.80f, 0.86f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
        }

        private void BuildGround()
        {
            LowPoly.Spawn(PrimitiveType.Plane, "Earth", new Vector3(0f, -0.05f, 0f), new Vector3(10f, 1f, 4.5f), RailroadMaterials.Earth, _root);
            LowPoly.Spawn(PrimitiveType.Plane, "Grass", new Vector3(0f, 0f, 0f), new Vector3(8.5f, 1f, 4f), RailroadMaterials.Grass, _root);
        }

        private void BuildDeck()
        {
            LowPoly.Spawn(PrimitiveType.Cube, "Deck", new Vector3(0f, DeckY * 0.5f, 0f), new Vector3(Width, DeckY, Length), RailroadMaterials.Stonebridge, _root);
            LowPoly.Spawn(PrimitiveType.Cube, "EdgeNear", new Vector3(-Width * 0.5f + 0.12f, DeckY + 0.08f, 0f), new Vector3(0.18f, 0.16f, Length), RailroadMaterials.TrainDark, _root);
            LowPoly.Spawn(PrimitiveType.Cube, "EdgeFar", new Vector3(Width * 0.5f - 0.12f, DeckY + 0.08f, 0f), new Vector3(0.18f, 0.16f, Length), RailroadMaterials.TrainDark, _root);

            int stripes = 10;
            for (int i = 0; i < stripes; i++)
            {
                float z = -Length * 0.5f + 1f + i * (Length - 2f) / (stripes - 1);
                LowPoly.Spawn(PrimitiveType.Cube, "Plank", new Vector3(0f, DeckY + 0.01f, z), new Vector3(Width - 0.5f, 0.04f, 0.55f), RailroadMaterials.Tie, _root);
            }
        }

        private void BuildRails()
        {
            LowPoly.Spawn(PrimitiveType.Cube, "Ballast", new Vector3(RailX, 0.08f, 0f), new Vector3(2.4f, 0.16f, Length + 2f), RailroadMaterials.Gravel, _root);

            int ties = 22;
            for (int i = 0; i <= ties; i++)
            {
                float z = -Length * 0.5f - 1f + i * (Length + 2f) / ties;
                LowPoly.Spawn(PrimitiveType.Cube, "Tie", new Vector3(RailX, 0.18f, z), new Vector3(1.8f, 0.1f, 0.22f), RailroadMaterials.Tie, _root);
            }

            LowPoly.Spawn(PrimitiveType.Cube, "RailL", new Vector3(RailX - 0.45f, 0.28f, 0f), new Vector3(0.12f, 0.12f, Length + 2f), RailroadMaterials.Rail, _root);
            LowPoly.Spawn(PrimitiveType.Cube, "RailR", new Vector3(RailX + 0.45f, 0.28f, 0f), new Vector3(0.12f, 0.12f, Length + 2f), RailroadMaterials.Rail, _root);
        }

        private void BuildTrain()
        {
            float railX = RailX;
            float trainZ = -2f;
            _trainRoot = new GameObject("Train").transform;
            _trainRoot.SetParent(_root, false);
            _trainRoot.position = new Vector3(railX, 0f, trainZ);
            _trainRoot.gameObject.AddComponent<KeepSeparate>();

            LowPoly.Child(PrimitiveType.Cube, "Engine", _trainRoot, new Vector3(0f, 0.85f, 2.4f), new Vector3(1.55f, 1.35f, 3.2f), RailroadMaterials.Train);
            LowPoly.Child(PrimitiveType.Cube, "Cabin", _trainRoot, new Vector3(0f, 1.45f, 1.5f), new Vector3(1.35f, 0.85f, 1.4f), RailroadMaterials.TrainDark);
            LowPoly.Child(PrimitiveType.Cylinder, "Stack", _trainRoot, new Vector3(0f, 1.85f, 3.2f), new Vector3(0.35f, 0.45f, 0.35f), RailroadMaterials.TrainDark);
            LowPoly.Child(PrimitiveType.Cube, "Cowcatcher", _trainRoot, new Vector3(0f, 0.35f, 3.9f), new Vector3(1.3f, 0.35f, 0.55f), RailroadMaterials.Rail);
            LowPoly.Child(PrimitiveType.Cube, "Car", _trainRoot, new Vector3(0f, 0.8f, -1.6f), new Vector3(1.5f, 1.2f, 4.2f), RailroadMaterials.TrainDark);
            LowPoly.Child(PrimitiveType.Cube, "CarRoof", _trainRoot, new Vector3(0f, 1.5f, -1.6f), new Vector3(1.55f, 0.2f, 4.3f), RailroadMaterials.Rail);
            LowPoly.Child(PrimitiveType.Cube, "Door", _trainRoot, new Vector3(0.78f, 0.85f, -0.4f), new Vector3(0.08f, 1.0f, 0.7f), RailroadMaterials.Millhaven);

            var seat = new GameObject("BoardSeat").transform;
            seat.SetParent(_trainRoot, false);
            seat.localPosition = new Vector3(0.15f, 1.35f, -0.4f);
            _boardSeat = seat;

            float padX = -WalkHalfWidth + 0.55f;
            float padZ = trainZ - 0.4f;
            LowPoly.Spawn(PrimitiveType.Cube, "BoardPad", new Vector3(padX, DeckY + 0.04f, padZ), new Vector3(1.6f, 0.08f, 2.4f), RailroadMaterials.Sun, _root);
            LowPoly.Spawn(PrimitiveType.Cube, "BoardMark", new Vector3(padX, DeckY + 0.1f, padZ), new Vector3(1.2f, 0.05f, 0.18f), RailroadMaterials.Train, _root);
            _boardingZone = new Bounds(new Vector3(padX, WalkY, padZ), new Vector3(1.7f, 1.2f, 2.5f));
        }

        private void BuildFurniture()
        {
            float[] benches = { -7f, 3f, 7f };
            for (int i = 0; i < benches.Length; i++)
            {
                float z = benches[i];
                float x = 1.7f;
                LowPoly.Spawn(PrimitiveType.Cube, "Bench", new Vector3(x, DeckY + 0.32f, z), new Vector3(1.4f, 0.18f, 0.55f), RailroadMaterials.Tie, _root);
                LowPoly.Spawn(PrimitiveType.Cube, "Back", new Vector3(x + 0.55f, DeckY + 0.55f, z), new Vector3(0.12f, 0.55f, 0.55f), RailroadMaterials.Tie, _root);
            }

            LowPoly.Spawn(PrimitiveType.Cylinder, "SignPost", new Vector3(-1.8f, DeckY + 1.1f, -9f), new Vector3(0.12f, 1.1f, 0.12f), RailroadMaterials.TrainDark, _root);
            LowPoly.Spawn(PrimitiveType.Cube, "Sign", new Vector3(-1.8f, DeckY + 2.15f, -9f), new Vector3(1.6f, 0.7f, 0.12f), RailroadMaterials.Portmere, _root);
        }

        private void BuildStationHouse()
        {
            float z = 9f;
            LowPoly.Spawn(PrimitiveType.Cube, "Station", new Vector3(1.4f, DeckY + 1.1f, z), new Vector3(3.4f, 2.2f, 3.2f), RailroadMaterials.Millhaven, _root);
            LowPoly.Spawn(PrimitiveType.Cube, "Door", new Vector3(-0.1f, DeckY + 0.85f, z - 1.55f), new Vector3(0.9f, 1.5f, 0.12f), RailroadMaterials.Shop, _root);

            float deskX = 0.2f;
            float deskZ = z - 3.4f;
            LowPoly.Spawn(PrimitiveType.Cube, "DeskPad", new Vector3(deskX, DeckY + 0.04f, deskZ), new Vector3(2.2f, 0.08f, 2.2f), RailroadMaterials.Portmere, _root);
            LowPoly.Spawn(PrimitiveType.Cube, "DeskMark", new Vector3(deskX, DeckY + 0.1f, deskZ), new Vector3(1.6f, 0.05f, 0.18f), RailroadMaterials.Lakeside, _root);
            _stationDeskZone = new Bounds(new Vector3(deskX, WalkY, deskZ), new Vector3(2.3f, 1.2f, 2.3f));
        }
    }
}
