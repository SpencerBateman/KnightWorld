using UnityEngine;
using UnityEngine.Rendering;

namespace Knightworld.Presentation
{
    public static class LowPoly
    {
        private static Mesh _cube;
        private static Mesh _sphere;
        private static Mesh _cylinder;
        private static Mesh _plane;
        private static Mesh _capsule;

        public static Mesh Cube => _cube != null ? _cube : (_cube = BuildCube());
        public static Mesh Sphere => _sphere != null ? _sphere : (_sphere = BuildOctahedron());
        public static Mesh Cylinder => _cylinder != null ? _cylinder : (_cylinder = BuildHexPrism());
        public static Mesh Plane => _plane != null ? _plane : (_plane = BuildPlane());
        public static Mesh Capsule => _capsule != null ? _capsule : (_capsule = BuildCapsule());

        public static Mesh MeshFor(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Sphere:
                    return Sphere;
                case PrimitiveType.Cylinder:
                    return Cylinder;
                case PrimitiveType.Capsule:
                    return Capsule;
                case PrimitiveType.Plane:
                    return Plane;
                default:
                    return Cube;
            }
        }

        public static GameObject Spawn(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool collider = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = MeshFor(type);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            if (collider)
                go.AddComponent<BoxCollider>();
            return go;
        }

        public static GameObject Child(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var go = Spawn(type, name, Vector3.zero, scale, material, parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            return go;
        }

        public static void Strip(Renderer renderer)
        {
            if (renderer == null)
                return;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private static Mesh BuildCube()
        {
            var mesh = new Mesh { name = "LP Cube" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5
            };
            Finish(mesh);
            return mesh;
        }

        private static Mesh BuildOctahedron()
        {
            var mesh = new Mesh { name = "LP Sphere" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0.5f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0f, 0f, -0.5f),
                new Vector3(0f, -0.5f, 0f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1,
                5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 1, 4
            };
            Finish(mesh);
            return mesh;
        }

        private static Mesh BuildHexPrism()
        {
            const int sides = 6;
            var verts = new Vector3[sides * 2 + 2];
            var tris = new int[sides * 12];
            for (int i = 0; i < sides; i++)
            {
                float ang = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(ang) * 0.5f;
                float z = Mathf.Sin(ang) * 0.5f;
                verts[i] = new Vector3(x, -1f, z);
                verts[i + sides] = new Vector3(x, 1f, z);
            }

            verts[sides * 2] = new Vector3(0f, -1f, 0f);
            verts[sides * 2 + 1] = new Vector3(0f, 1f, 0f);
            int t = 0;
            int bottom = sides * 2;
            int top = sides * 2 + 1;
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                tris[t++] = i;
                tris[t++] = n;
                tris[t++] = i + sides;
                tris[t++] = n;
                tris[t++] = n + sides;
                tris[t++] = i + sides;
                tris[t++] = bottom;
                tris[t++] = n;
                tris[t++] = i;
                tris[t++] = top;
                tris[t++] = i + sides;
                tris[t++] = n + sides;
            }

            var mesh = new Mesh { name = "LP Cylinder" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            Finish(mesh);
            return mesh;
        }

        private static Mesh BuildPlane()
        {
            var mesh = new Mesh { name = "LP Plane" };
            mesh.vertices = new[]
            {
                new Vector3(-5f, 0f, -5f),
                new Vector3(5f, 0f, -5f),
                new Vector3(5f, 0f, 5f),
                new Vector3(-5f, 0f, 5f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            Finish(mesh);
            return mesh;
        }

        private static Mesh BuildCapsule()
        {
            var mesh = new Mesh { name = "LP Capsule" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 1f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(0f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0f, 0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0f, -0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0f, -0.5f, -0.5f),
                new Vector3(0f, -1f, 0f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1,
                1, 5, 6, 1, 6, 2, 2, 6, 7, 2, 7, 3, 3, 7, 8, 3, 8, 4, 4, 8, 5, 4, 5, 1,
                9, 6, 5, 9, 7, 6, 9, 8, 7, 9, 5, 8
            };
            Finish(mesh);
            return mesh;
        }

        private static void Finish(Mesh mesh)
        {
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
        }
    }
}
