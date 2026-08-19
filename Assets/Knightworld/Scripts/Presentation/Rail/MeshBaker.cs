using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Knightworld.Presentation
{
    public sealed class KeepSeparate : MonoBehaviour
    {
    }

    public static class MeshBaker
    {
        public static void Bake(Transform root)
        {
            if (root == null)
                return;
            var drifts = root.GetComponentsInChildren<CloudDrift>(true);
            for (int i = 0; i < drifts.Length; i++)
                CombineGroup(drifts[i].transform, true);
            CombineGroup(root, false);
        }

        private static void CombineGroup(Transform host, bool localToHost)
        {
            var filters = host.GetComponentsInChildren<MeshFilter>(true);
            var groups = new Dictionary<Material, List<MeshFilter>>();
            for (int i = 0; i < filters.Length; i++)
            {
                var filter = filters[i];
                if (filter.sharedMesh == null || !CanBake(filter.gameObject, host, localToHost))
                    continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.sharedMaterial == null)
                    continue;
                var material = renderer.sharedMaterial;
                if (!groups.TryGetValue(material, out var list))
                {
                    list = new List<MeshFilter>();
                    groups[material] = list;
                }

                list.Add(filter);
            }

            foreach (var pair in groups)
            {
                if (pair.Value.Count < 2)
                    continue;
                var mesh = Combine(pair.Value, localToHost ? host : null);
                if (mesh == null)
                    continue;
                var batch = new GameObject(pair.Key.name + " Batch");
                batch.transform.SetParent(host, false);
                if (!localToHost)
                {
                    batch.transform.position = Vector3.zero;
                    batch.transform.rotation = Quaternion.identity;
                    batch.transform.localScale = Vector3.one;
                }

                batch.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = batch.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key;
                LowPoly.Strip(renderer);
                for (int i = 0; i < pair.Value.Count; i++)
                    Object.Destroy(pair.Value[i].gameObject);
            }
        }

        private static bool CanBake(GameObject go, Transform host, bool localToHost)
        {
            if (go == host.gameObject)
                return false;
            if (go.GetComponent<Collider>() != null)
                return false;
            if (go.GetComponent<TextMesh>() != null)
                return false;
            var behaviours = go.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                    return false;
            }

            if (!localToHost)
            {
                var parent = go.transform.parent;
                while (parent != null && parent != host)
                {
                    if (parent.GetComponent<MonoBehaviour>() != null)
                        return false;
                    parent = parent.parent;
                }
            }

            return true;
        }

        private static Mesh Combine(List<MeshFilter> filters, Transform host)
        {
            var combines = new CombineInstance[filters.Count];
            int verts = 0;
            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                verts += filter.sharedMesh.vertexCount;
                Matrix4x4 matrix = filter.transform.localToWorldMatrix;
                if (host != null)
                    matrix = host.worldToLocalMatrix * matrix;
                combines[i] = new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    transform = matrix
                };
            }

            var mesh = new Mesh { name = "Batch" };
            if (verts > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.CombineMeshes(combines, true, true);
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
