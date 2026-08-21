using System.Collections.Generic;
using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class RailroadView
    {
        public const float WorldScale = 5f;
        public const float TrainY = 0.42f;

        private readonly Transform _root;
        private readonly Dictionary<int, Transform> _waiting = new Dictionary<int, Transform>();
        private readonly List<Renderer> _seats = new List<Renderer>();
        private readonly List<Transform> _labels = new List<Transform>();
        private Transform _train;
        private Transform _passengerRoot;
        private Transform _seatRoot;
        private Transform _car;
        private Transform _sceneryRoot;
        private Transform _railRoot;
        private RailroadScenery _scenery;
        private readonly Dictionary<string, DestPin> _destPins = new Dictionary<string, DestPin>();
        private readonly Dictionary<string, GameObject> _lockedTracks = new Dictionary<string, GameObject>();

        public RailroadView(Transform root)
        {
            _root = root;
        }

        public Vector3 Center { get; private set; }
        public float Radius { get; private set; }
        public Transform Train => _train;

        public void Build()
        {
            RailroadMaterials.Ensure();
            _sceneryRoot = new GameObject("Scenery").transform;
            _sceneryRoot.SetParent(_root, false);
            _scenery = new RailroadScenery(_sceneryRoot, this);
            _scenery.Build();
            _railRoot = new GameObject("Rails").transform;
            _railRoot.SetParent(_root, false);
            DrawRails();
            DrawTowns();
            MeshBaker.Bake(_root);
            _passengerRoot = new GameObject("Passengers").transform;
            _passengerRoot.SetParent(_root, false);
            _passengerRoot.gameObject.AddComponent<KeepSeparate>();
            _train = SpawnTrain();
            SnapTrain(RailroadGraph.StartTownId);
            Vector3 sum = Vector3.zero;
            foreach (var town in RailroadGraph.Towns)
                sum += WorldPos(town);
            Center = sum / RailroadGraph.Towns.Count;
            float radius = 0f;
            foreach (var town in RailroadGraph.Towns)
            {
                float d = Vector3.Distance(WorldPos(town), Center);
                if (d > radius)
                    radius = d;
            }
            Radius = radius;
        }

        public void ApplyCamera(Camera camera)
        {
            if (_scenery != null)
                _scenery.ApplyCamera(camera);
        }

        public Vector3 WorldPos(TownDef town) => new Vector3(town.X * WorldScale, 0f, town.Z * WorldScale);

        public Vector3 TrainPos(string townId) => WorldPos(RailroadGraph.Get(townId)) + Vector3.up * TrainY;

        public void SnapTrain(string townId)
        {
            if (_train == null)
                return;
            _train.position = TrainPos(townId);
        }

        public void FaceLabels(Camera camera)
        {
            if (camera == null)
                return;
            for (int i = 0; i < _labels.Count; i++)
            {
                Vector3 away = _labels[i].position - camera.transform.position;
                if (away.sqrMagnitude > 0.001f)
                    _labels[i].rotation = Quaternion.LookRotation(away);
            }
        }

        public void RefreshPassengers(RailSession session)
        {
            var keep = new HashSet<int>();
            foreach (var town in RailroadGraph.Towns)
            {
                var waiting = session.Waiting[town.Id];
                Vector3 origin = WorldPos(town);
                Vector3 side = new Vector3(-0.85f, 0f, 1.55f);
                for (int i = 0; i < waiting.Count; i++)
                {
                    var person = waiting[i];
                    keep.Add(person.Id);
                    if (!_waiting.TryGetValue(person.Id, out var body))
                    {
                        body = SpawnPerson(person);
                        _waiting[person.Id] = body;
                    }

                    body.position = origin + side + new Vector3(0.7f * i, WorldLayers.Person, 0f);
                }
            }

            var remove = new List<int>();
            foreach (var pair in _waiting)
            {
                if (keep.Contains(pair.Key))
                    continue;
                Object.Destroy(pair.Value.gameObject);
                remove.Add(pair.Key);
            }

            for (int i = 0; i < remove.Count; i++)
                _waiting.Remove(remove[i]);

            SyncSeats(session.SeatCount);
            for (int i = 0; i < _seats.Count; i++)
            {
                if (i < session.Onboard.Count)
                    _seats[i].sharedMaterial = RailroadMaterials.Town(session.Onboard[i].DestId);
                else
                    _seats[i].sharedMaterial = RailroadMaterials.SeatEmpty;
            }

            RefreshDestPins(session);
        }

        private Transform SpawnPerson(Passenger person)
        {
            var go = new GameObject(person.Name);
            go.transform.SetParent(_passengerRoot, false);
            var marker = go.AddComponent<PassengerMarker>();
            marker.PassengerId = person.Id;
            var hit = go.AddComponent<SphereCollider>();
            hit.radius = 0.42f;

            var body = LowPoly.Child(PrimitiveType.Capsule, "Body", go.transform, Vector3.zero, new Vector3(0.32f, 0.38f, 0.32f), person.IsQuest ? RailroadMaterials.Sun : RailroadMaterials.Town(person.DestId));
            return go.transform;
        }

        private Transform SpawnTrain()
        {
            var root = new GameObject("Train").transform;
            root.SetParent(_root, false);
            root.gameObject.AddComponent<KeepSeparate>();

            var engine = LowPoly.Child(PrimitiveType.Cube, "Engine", root, new Vector3(0f, 0.28f, 0.55f), new Vector3(0.7f, 0.55f, 1.1f), RailroadMaterials.Train);

            var cabin = LowPoly.Child(PrimitiveType.Cube, "Cabin", root, new Vector3(0f, 0.55f, 0.25f), new Vector3(0.62f, 0.42f, 0.5f), RailroadMaterials.TrainDark);

            var stack = LowPoly.Child(PrimitiveType.Cylinder, "Stack", root, new Vector3(0f, 0.72f, 0.85f), new Vector3(0.16f, 0.18f, 0.16f), RailroadMaterials.TrainDark);

            var car = LowPoly.Child(PrimitiveType.Cube, "Car", root, Vector3.zero, Vector3.one, RailroadMaterials.Train);
            _car = car.transform;
            _seatRoot = root;
            SyncSeats(RailSession.StartingSeats);
            return root;
        }

        public void SyncSeats(int seatCount)
        {
            if (_seatRoot == null)
                return;
            while (_seats.Count < seatCount)
                AddSeatVisual(_seats.Count);
            if (_car != null)
            {
                float length = 0.6f + seatCount * 0.28f;
                _car.localScale = new Vector3(0.72f, 0.38f, length);
                _car.localPosition = new Vector3(0f, 0.32f, -0.25f - length * 0.35f);
                _car.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Train;
            }
        }

        private void AddSeatVisual(int index)
        {
            int row = index / 4;
            int col = index % 4;
            var seat = LowPoly.Child(PrimitiveType.Cube, "Seat " + index, _seatRoot, new Vector3(-0.22f + col * 0.14f, 0.58f, -0.4f - row * 0.38f), new Vector3(0.12f, 0.12f, 0.16f), RailroadMaterials.SeatEmpty);
            _seats.Add(seat.GetComponent<Renderer>());
        }

        private void DrawRails()
        {
            var drawn = new HashSet<string>();
            foreach (var town in RailroadGraph.Towns)
            {
                foreach (var link in town.Links)
                {
                    string key = RailroadMap.TrackKey(town.Id, link);
                    if (!drawn.Add(key))
                        continue;
                    Vector3 a = WorldPos(town);
                    Vector3 b = WorldPos(RailroadGraph.Get(link));
                    if (RailroadGraph.IsLocked(town.Id, link))
                        DrawLockedTrack(key, a, b);
                    else
                        DrawTrack(a, b, _railRoot);
                }
            }
        }

        public void UnlockTrack(string fromId, string toId)
        {
            string key = RailroadMap.TrackKey(fromId, toId);
            if (_lockedTracks.TryGetValue(key, out var ghost))
            {
                Object.Destroy(ghost);
                _lockedTracks.Remove(key);
            }

            var group = new GameObject("Track").transform;
            group.SetParent(_railRoot != null ? _railRoot : _root, false);
            DrawTrack(WorldPos(RailroadGraph.Get(fromId)), WorldPos(RailroadGraph.Get(toId)), group);
            MeshBaker.Bake(group);
        }

        private void DrawLockedTrack(string key, Vector3 a, Vector3 b)
        {
            Vector3 delta = b - a;
            float length = delta.magnitude;
            Quaternion facing = Quaternion.LookRotation(delta.sqrMagnitude > 0.001f ? delta : Vector3.forward);

            var root = new GameObject("Locked " + key);
            root.transform.SetParent(_root, false);
            root.AddComponent<KeepSeparate>();
            _lockedTracks[key] = root;

            int dashes = Mathf.Max(3, Mathf.RoundToInt(length / 1.15f));
            for (int i = 0; i < dashes; i++)
            {
                float t = (i + 0.5f) / dashes;
                var dash = LowPoly.Spawn(PrimitiveType.Cube, "Dash", WorldLayers.Lift(Vector3.Lerp(a, b, t), WorldLayers.Ballast), new Vector3(0.38f, WorldLayers.BallastThick, length / dashes * 0.45f), RailroadMaterials.LockedRail, root.transform);
                dash.transform.rotation = facing;
            }

            MeshBaker.Bake(root.transform);
        }

        private void DrawTrack(Vector3 a, Vector3 b, Transform parent)
        {
            Vector3 mid = (a + b) * 0.5f;
            Vector3 delta = b - a;
            float length = delta.magnitude;
            Quaternion facing = Quaternion.LookRotation(delta.sqrMagnitude > 0.001f ? delta : Vector3.forward);
            if (parent == null)
                parent = _root;

            var bed = LowPoly.Spawn(PrimitiveType.Cube, "Ballast", WorldLayers.Lift(mid, WorldLayers.Ballast), new Vector3(0.95f, WorldLayers.BallastThick, length), RailroadMaterials.Gravel, parent);
            bed.transform.rotation = facing;

            int ties = Mathf.Max(2, Mathf.RoundToInt(length / 0.85f));
            for (int i = 0; i <= ties; i++)
            {
                float t = i / (float)ties;
                var tie = LowPoly.Spawn(PrimitiveType.Cube, "Tie", WorldLayers.Lift(Vector3.Lerp(a, b, t), WorldLayers.Tie, i), new Vector3(0.72f, WorldLayers.TieThick, 0.14f), RailroadMaterials.Tie, parent);
                tie.transform.rotation = facing;
            }

            var rails = LowPoly.Spawn(PrimitiveType.Cube, "Rails", WorldLayers.Lift(mid, WorldLayers.Rail), new Vector3(0.42f, WorldLayers.RailThick, length), RailroadMaterials.Rail, parent);
            rails.transform.rotation = facing;
        }

        private void DrawTowns()
        {
            foreach (var town in RailroadGraph.Towns)
            {
                var pad = LowPoly.Spawn(PrimitiveType.Cylinder, town.Name, WorldLayers.Lift(WorldPos(town), WorldLayers.Platform), new Vector3(2.1f, WorldLayers.PlatformHalf, 2.1f), RailroadMaterials.Town(town.Id), _root);

                House(WorldPos(town) + new Vector3(-0.55f, WorldLayers.House, -0.35f), RailroadMaterials.Town(town.Id));
                House(WorldPos(town) + new Vector3(0.45f, WorldLayers.HouseLow, -0.55f), RailroadMaterials.TrainDark);
                Store(WorldPos(town) + new Vector3(1.15f, WorldLayers.Store, 0.55f), RailroadMaterials.Town(town.Id), town.Id);

                var hit = new GameObject("Hit " + town.Id);
                hit.transform.SetParent(_root, false);
                hit.transform.position = WorldPos(town) + Vector3.up * 0.55f;
                var sphere = hit.AddComponent<SphereCollider>();
                sphere.radius = 1.05f;
                var marker = hit.AddComponent<TownMarker>();
                marker.TownId = town.Id;

                var label = new GameObject("Label " + town.Name);
                label.transform.SetParent(_root, false);
                label.transform.position = WorldPos(town) + Vector3.up * 2.55f;
                var mesh = label.AddComponent<TextMesh>();
                mesh.text = town.Name;
                mesh.fontSize = 48;
                mesh.characterSize = 0.12f;
                mesh.anchor = TextAnchor.LowerCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.color = Color.white;
                var labelRenderer = label.GetComponent<MeshRenderer>();
                if (labelRenderer != null)
                    labelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _labels.Add(label.transform);
                SpawnDestPin(town);
            }
        }

        private void SpawnDestPin(TownDef town)
        {
            var root = new GameObject("DestPin " + town.Id);
            root.transform.SetParent(_root, false);
            root.transform.position = WorldPos(town) + Vector3.up * 3.35f;

            var floater = new GameObject("Float");
            floater.transform.SetParent(root.transform, false);
            var bob = floater.AddComponent<DestBeacon>();
            bob.Phase = town.Id.Length * 0.7f;

            var face = new GameObject("Face");
            face.transform.SetParent(floater.transform, false);
            _labels.Add(face.transform);

            var gem = LowPoly.Child(PrimitiveType.Sphere, "Gem", face.transform, Vector3.zero, new Vector3(0.52f, 0.34f, 0.52f), RailroadMaterials.Town(town.Id));

            var count = new GameObject("Count");
            count.transform.SetParent(face.transform, false);
            count.transform.localPosition = new Vector3(0f, 0.02f, -0.02f);
            var mesh = count.AddComponent<TextMesh>();
            mesh.text = "1";
            mesh.fontSize = 64;
            mesh.characterSize = 0.06f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            var countRenderer = count.GetComponent<MeshRenderer>();
            if (countRenderer != null)
                countRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            root.SetActive(false);
            _destPins[town.Id] = new DestPin
            {
                Root = root,
                Count = mesh,
                Gem = gem.GetComponent<Renderer>()
            };
        }

        private void RefreshDestPins(RailSession session)
        {
            foreach (var town in RailroadGraph.Towns)
            {
                if (!_destPins.TryGetValue(town.Id, out var pin))
                    continue;
                int count = session.CountOnboardTo(town.Id);
                pin.Root.SetActive(count > 0);
                if (count <= 0)
                    continue;
                pin.Count.text = count.ToString();
                pin.Gem.sharedMaterial = RailroadMaterials.Town(town.Id);
            }
        }

        private struct DestPin
        {
            public GameObject Root;
            public TextMesh Count;
            public Renderer Gem;
        }

        private void House(Vector3 position, Material material)
        {
            LowPoly.Spawn(PrimitiveType.Cube, "House", position, new Vector3(0.7f, 0.7f, 0.55f), material, _root);
        }

        private void Store(Vector3 position, Material awning, string townId)
        {
            var stall = LowPoly.Spawn(PrimitiveType.Cube, "Store", position, new Vector3(0.85f, 0.55f, 0.7f), RailroadMaterials.Shop, _root, true);
            var marker = stall.AddComponent<ShopMarker>();
            marker.TownId = townId;
            LowPoly.Spawn(PrimitiveType.Cube, "Awning", position + new Vector3(0f, 0.42f, 0.12f), new Vector3(1.05f, 0.08f, 0.95f), awning, _root);
            LowPoly.Spawn(PrimitiveType.Cube, "Crate", position + new Vector3(0.55f, -0.12f, 0.15f), new Vector3(0.28f, 0.28f, 0.28f), RailroadMaterials.Tie, _root);
        }
    }
}
