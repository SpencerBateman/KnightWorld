using System.Collections.Generic;
using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class RailroadView
    {
        public const float WorldScale = 1.65f;
        public const float TrainY = 0.42f;

        private readonly Transform _root;
        private readonly Dictionary<int, Transform> _waiting = new Dictionary<int, Transform>();
        private readonly List<Renderer> _seats = new List<Renderer>();
        private readonly List<Transform> _labels = new List<Transform>();
        private Transform _train;
        private Transform _passengerRoot;
        private Transform _seatRoot;
        private Transform _car;
        private RailroadScenery _scenery;

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
            _scenery = new RailroadScenery(_root, this);
            _scenery.Build();
            DrawRails();
            DrawTowns();
            _passengerRoot = new GameObject("Passengers").transform;
            _passengerRoot.SetParent(_root, false);
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

                    body.position = origin + side + new Vector3(0.7f * i, 0.45f, 0f);
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
        }

        private Transform SpawnPerson(Passenger person)
        {
            var go = new GameObject(person.Name);
            go.transform.SetParent(_passengerRoot, false);
            var marker = go.AddComponent<PassengerMarker>();
            marker.PassengerId = person.Id;
            var hit = go.AddComponent<SphereCollider>();
            hit.radius = 0.42f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.32f, 0.38f, 0.32f);
            body.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Town(person.DestId);
            Object.Destroy(body.GetComponent<Collider>());
            return go.transform;
        }

        private Transform SpawnTrain()
        {
            var root = new GameObject("Train").transform;
            root.SetParent(_root, false);

            var engine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            engine.name = "Engine";
            engine.transform.SetParent(root, false);
            engine.transform.localPosition = new Vector3(0f, 0.28f, 0.55f);
            engine.transform.localScale = new Vector3(0.7f, 0.55f, 1.1f);
            engine.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Train;
            Object.Destroy(engine.GetComponent<Collider>());

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(root, false);
            cabin.transform.localPosition = new Vector3(0f, 0.55f, 0.25f);
            cabin.transform.localScale = new Vector3(0.62f, 0.42f, 0.5f);
            cabin.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.TrainDark;
            Object.Destroy(cabin.GetComponent<Collider>());

            var stack = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stack.name = "Stack";
            stack.transform.SetParent(root, false);
            stack.transform.localPosition = new Vector3(0f, 0.72f, 0.85f);
            stack.transform.localScale = new Vector3(0.16f, 0.18f, 0.16f);
            stack.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.TrainDark;
            Object.Destroy(stack.GetComponent<Collider>());

            var car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "Car";
            car.transform.SetParent(root, false);
            _car = car.transform;
            Object.Destroy(car.GetComponent<Collider>());
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
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat " + index;
            seat.transform.SetParent(_seatRoot, false);
            seat.transform.localPosition = new Vector3(-0.22f + col * 0.14f, 0.58f, -0.4f - row * 0.38f);
            seat.transform.localScale = new Vector3(0.12f, 0.12f, 0.16f);
            seat.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.SeatEmpty;
            Object.Destroy(seat.GetComponent<Collider>());
            _seats.Add(seat.GetComponent<Renderer>());
        }

        private void DrawRails()
        {
            var drawn = new HashSet<string>();
            foreach (var town in RailroadGraph.Towns)
            {
                foreach (var link in town.Links)
                {
                    string key = town.Id.CompareTo(link) < 0 ? town.Id + ">" + link : link + ">" + town.Id;
                    if (!drawn.Add(key))
                        continue;
                    Vector3 a = WorldPos(town) + Vector3.up * 0.05f;
                    Vector3 b = WorldPos(RailroadGraph.Get(link)) + Vector3.up * 0.05f;
                    DrawTrack(a, b);
                }
            }
        }

        private void DrawTrack(Vector3 a, Vector3 b)
        {
            Vector3 delta = b - a;
            float length = delta.magnitude;
            var rails = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rails.name = "Rails";
            rails.transform.SetParent(_root, false);
            rails.transform.position = (a + b) * 0.5f;
            rails.transform.rotation = Quaternion.LookRotation(delta.sqrMagnitude > 0.001f ? delta : Vector3.forward);
            rails.transform.localScale = new Vector3(0.42f, 0.06f, length);
            rails.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Rail;
            Object.Destroy(rails.GetComponent<Collider>());

            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Ballast";
            bed.transform.SetParent(_root, false);
            bed.transform.position = (a + b) * 0.5f + Vector3.down * 0.02f;
            bed.transform.rotation = rails.transform.rotation;
            bed.transform.localScale = new Vector3(0.95f, 0.05f, length);
            bed.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Gravel;
            Object.Destroy(bed.GetComponent<Collider>());

            int ties = Mathf.Max(2, Mathf.RoundToInt(length / 0.85f));
            for (int i = 0; i <= ties; i++)
            {
                float t = i / (float)ties;
                var tie = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tie.name = "Tie";
                tie.transform.SetParent(_root, false);
                tie.transform.position = Vector3.Lerp(a, b, t);
                tie.transform.rotation = rails.transform.rotation;
                tie.transform.localScale = new Vector3(0.72f, 0.05f, 0.14f);
                tie.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Tie;
                Object.Destroy(tie.GetComponent<Collider>());
            }
        }

        private void DrawTowns()
        {
            foreach (var town in RailroadGraph.Towns)
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = town.Name;
                pad.transform.SetParent(_root, false);
                pad.transform.position = WorldPos(town) + Vector3.up * 0.08f;
                pad.transform.localScale = new Vector3(2.1f, 0.08f, 2.1f);
                pad.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Town(town.Id);
                Object.Destroy(pad.GetComponent<Collider>());

                House(WorldPos(town) + new Vector3(-0.55f, 0.45f, -0.35f), RailroadMaterials.Town(town.Id));
                House(WorldPos(town) + new Vector3(0.45f, 0.38f, -0.55f), RailroadMaterials.TrainDark);
                Store(WorldPos(town) + new Vector3(1.15f, 0.42f, 0.55f), RailroadMaterials.Town(town.Id), town.Id);

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
            }
        }

        private void House(Vector3 position, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "House";
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 0.55f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(go.GetComponent<Collider>());
        }

        private void Store(Vector3 position, Material awning, string townId)
        {
            var stall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stall.name = "Store";
            stall.transform.SetParent(_root, false);
            stall.transform.position = position;
            stall.transform.localScale = new Vector3(0.85f, 0.55f, 0.7f);
            stall.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Shop;
            var marker = stall.AddComponent<ShopMarker>();
            marker.TownId = townId;

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Awning";
            roof.transform.SetParent(_root, false);
            roof.transform.position = position + new Vector3(0f, 0.42f, 0.12f);
            roof.transform.localScale = new Vector3(1.05f, 0.08f, 0.95f);
            roof.GetComponent<Renderer>().sharedMaterial = awning;
            Object.Destroy(roof.GetComponent<Collider>());

            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.SetParent(_root, false);
            crate.transform.position = position + new Vector3(0.55f, -0.12f, 0.15f);
            crate.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
            crate.GetComponent<Renderer>().sharedMaterial = RailroadMaterials.Tie;
            Object.Destroy(crate.GetComponent<Collider>());
        }
    }
}
