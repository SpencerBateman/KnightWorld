using System.Collections.Generic;
using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class OverworldView
    {
        public const float WorldScale = 1.7f;
        public const float TokenY = 0.85f;

        private readonly Transform _root;
        private readonly Dictionary<string, Transform> _pads = new Dictionary<string, Transform>();
        private readonly List<Transform> _labels = new List<Transform>();
        private Transform _token;
        private Renderer _tokenRenderer;

        public OverworldView(Transform root)
        {
            _root = root;
        }

        public Vector3 Center { get; private set; }

        public Transform Token => _token;

        public void Build()
        {
            PlaceholderMaterials.Ensure();
            DrawGround();
            DrawDecor();
            DrawPaths();
            DrawNodes();
            _token = SpawnToken();
            SnapToken(CampaignState.CurrentNodeId);
            Vector3 sum = Vector3.zero;
            foreach (var node in OverworldGraph.Nodes)
                sum += WorldPos(node);
            Center = sum / OverworldGraph.Nodes.Count;
            RefreshNodes();
        }

        public Vector3 WorldPos(OverworldNode node)
        {
            return new Vector3(node.X * WorldScale, 0f, node.Z * WorldScale);
        }

        public Vector3 TokenPos(string nodeId) => WorldPos(OverworldGraph.Get(nodeId)) + Vector3.up * TokenY;

        public void SnapToken(string nodeId)
        {
            if (_token == null)
                return;
            _token.position = TokenPos(nodeId);
        }

        public void RefreshNodes()
        {
            foreach (var node in OverworldGraph.Nodes)
            {
                if (!_pads.TryGetValue(node.Id, out var pad))
                    continue;
                var renderer = pad.GetComponent<Renderer>();
                if (renderer == null)
                    continue;
                bool current = node.Id == CampaignState.CurrentNodeId;
                bool done = CampaignState.Completed.Contains(node.Id);
                bool open = CampaignState.IsUnlocked(node.Id);
                if (current)
                    renderer.sharedMaterial = PlaceholderMaterials.OverworldCurrent;
                else if (done)
                    renderer.sharedMaterial = PlaceholderMaterials.OverworldClear;
                else if (open)
                    renderer.sharedMaterial = PlaceholderMaterials.OverworldOpen;
                else
                    renderer.sharedMaterial = PlaceholderMaterials.OverworldLocked;

                var gem = pad.Find("Gem");
                if (gem != null)
                {
                    gem.gameObject.SetActive(open);
                    var gemRenderer = gem.GetComponent<Renderer>();
                    if (gemRenderer != null)
                    {
                        gemRenderer.sharedMaterial = done
                            ? PlaceholderMaterials.OverworldClear
                            : (current ? PlaceholderMaterials.OverworldCurrent : PlaceholderMaterials.OverworldOpen);
                    }
                }
            }

            if (_tokenRenderer != null)
                _tokenRenderer.sharedMaterial = PlaceholderMaterials.OverworldToken;
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

        private Transform SpawnToken()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Knight Token";
            go.transform.SetParent(_root, false);
            go.transform.localScale = new Vector3(0.42f, 0.62f, 0.42f);
            Object.Destroy(go.GetComponent<Collider>());
            _tokenRenderer = go.GetComponent<Renderer>();
            _tokenRenderer.sharedMaterial = PlaceholderMaterials.OverworldToken;

            var helm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helm.name = "Helm";
            helm.transform.SetParent(go.transform, false);
            helm.transform.localPosition = new Vector3(0f, 0.55f, 0.12f);
            helm.transform.localScale = new Vector3(0.7f, 0.45f, 0.7f);
            Object.Destroy(helm.GetComponent<Collider>());
            helm.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.OverworldToken;
            return go.transform;
        }

        private void DrawGround()
        {
            var field = GameObject.CreatePrimitive(PrimitiveType.Plane);
            field.name = "OverworldGround";
            field.transform.SetParent(_root, false);
            field.transform.position = new Vector3(3.5f * WorldScale, 0f, 3.2f * WorldScale);
            field.transform.localScale = new Vector3(4.4f, 1f, 4.2f);
            field.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.OverworldGrass;
            Object.Destroy(field.GetComponent<Collider>());
        }

        private void DrawDecor()
        {
            Hill(new Vector3(-6f, -0.6f, -4f), new Vector3(5.5f, 2.2f, 4.5f));
            Hill(new Vector3(16f, -0.4f, 2f), new Vector3(4.5f, 1.6f, 4f));
            Hill(new Vector3(2f, -0.5f, 15f), new Vector3(6f, 2.4f, 5f));
            Hill(new Vector3(14f, -0.7f, 14f), new Vector3(4f, 1.8f, 3.5f));

            Pond(WorldPos(OverworldGraph.Get(OverworldGraph.Lakeshore)) + new Vector3(2.4f, 0.02f, 0.4f), 2.4f);
            Pond(new Vector3(-2.5f, 0.02f, 8f), 1.4f);

            RuinBlock(WorldPos(OverworldGraph.Get(OverworldGraph.Ruins)) + new Vector3(-1.6f, 0.45f, 1.3f), new Vector3(1.1f, 0.9f, 0.55f));
            RuinBlock(WorldPos(OverworldGraph.Get(OverworldGraph.Ruins)) + new Vector3(-0.7f, 0.35f, 2.1f), new Vector3(0.5f, 0.7f, 0.5f));

            Tree(new Vector3(-2.2f, 0f, -2.4f));
            Tree(new Vector3(4.2f, 0f, -3.1f));
            Tree(new Vector3(11.5f, 0f, 7.6f));
            Tree(new Vector3(-1.8f, 0f, 4.5f));
            Tree(new Vector3(8.8f, 0f, -1.6f));
        }

        private void DrawPaths()
        {
            var drawn = new HashSet<string>();
            foreach (var node in OverworldGraph.Nodes)
            {
                foreach (var link in node.Links)
                {
                    string key = node.Id.CompareTo(link) < 0 ? node.Id + ">" + link : link + ">" + node.Id;
                    if (!drawn.Add(key))
                        continue;
                    var a = WorldPos(node) + Vector3.up * 0.06f;
                    var b = WorldPos(OverworldGraph.Get(link)) + Vector3.up * 0.06f;
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Path " + key;
                    go.transform.SetParent(_root, false);
                    Vector3 delta = b - a;
                    go.transform.position = (a + b) * 0.5f;
                    go.transform.rotation = Quaternion.LookRotation(delta.sqrMagnitude > 0.001f ? delta : Vector3.forward);
                    go.transform.localScale = new Vector3(0.58f, 0.1f, delta.magnitude);
                    go.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.OverworldDirt;
                    Object.Destroy(go.GetComponent<Collider>());
                }
            }
        }

        private void DrawNodes()
        {
            foreach (var node in OverworldGraph.Nodes)
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = node.Title;
                pad.transform.SetParent(_root, false);
                pad.transform.position = WorldPos(node) + Vector3.up * 0.08f;
                pad.transform.localScale = new Vector3(1.55f, 0.08f, 1.55f);
                Object.Destroy(pad.GetComponent<Collider>());
                pad.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.OverworldOpen;

                var gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gem.name = "Gem";
                gem.transform.SetParent(pad.transform, false);
                gem.transform.localPosition = new Vector3(0f, 4.2f, 0f);
                gem.transform.localScale = new Vector3(0.32f, 2.8f, 0.32f);
                Object.Destroy(gem.GetComponent<Collider>());

                var hit = new GameObject("Hit " + node.Id);
                hit.transform.SetParent(_root, false);
                hit.transform.position = WorldPos(node) + Vector3.up * 0.7f;
                var sphere = hit.AddComponent<SphereCollider>();
                sphere.radius = 0.95f;
                var marker = hit.AddComponent<OverworldNodeMarker>();
                marker.NodeId = node.Id;

                var label = new GameObject("Label " + node.Title);
                label.transform.SetParent(_root, false);
                label.transform.position = WorldPos(node) + Vector3.up * 1.75f;
                var mesh = label.AddComponent<TextMesh>();
                mesh.text = node.Title;
                mesh.fontSize = 48;
                mesh.characterSize = 0.11f;
                mesh.anchor = TextAnchor.LowerCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.color = Color.white;
                var labelRenderer = label.GetComponent<MeshRenderer>();
                if (labelRenderer != null)
                    labelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _labels.Add(label.transform);

                _pads[node.Id] = pad.transform;
            }
        }

        private void Hill(Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Hill";
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.OverworldHill;
            Object.Destroy(go.GetComponent<Collider>());
        }

        private void Pond(Vector3 position, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Pond";
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 1.6f);
            go.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.OverworldWater;
            Object.Destroy(go.GetComponent<Collider>());
        }

        private void RuinBlock(Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Ruin";
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
            go.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.OverworldStone;
            Object.Destroy(go.GetComponent<Collider>());
        }

        private void Tree(Vector3 position)
        {
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Tree";
            trunk.transform.SetParent(_root, false);
            trunk.transform.position = position + Vector3.up * 0.55f;
            trunk.transform.localScale = new Vector3(0.28f, 0.55f, 0.28f);
            trunk.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.Bark;
            Object.Destroy(trunk.GetComponent<Collider>());

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(trunk.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            canopy.transform.localScale = new Vector3(3.2f, 2.4f, 3.2f);
            canopy.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.Leaves;
            Object.Destroy(canopy.GetComponent<Collider>());
        }
    }
}
