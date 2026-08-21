using UnityEngine;

namespace Knightworld.Presentation
{
    public static class PlatformFigure
    {
        public static GameObject Spawn(string name, Transform parent, Vector3 at, Material body, Material torso, Material head)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            LowPoly.Child(PrimitiveType.Capsule, "Body", go.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.55f, 0.7f, 0.55f), body);
            LowPoly.Child(PrimitiveType.Cube, "Torso", go.transform, new Vector3(0f, 1.05f, 0.02f), new Vector3(0.62f, 0.55f, 0.4f), torso);
            LowPoly.Child(PrimitiveType.Sphere, "Head", go.transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.42f, 0.42f, 0.42f), head);
            LowPoly.Child(PrimitiveType.Cube, "Nose", go.transform, new Vector3(0f, 1.5f, 0.22f), new Vector3(0.12f, 0.1f, 0.18f), head);
            return go;
        }

        public static GameObject SpawnPlayer(Transform parent, Vector3 at)
        {
            RailroadMaterials.Ensure();
            return Spawn("Walker", parent, at, RailroadMaterials.Emberford, RailroadMaterials.Train, RailroadMaterials.Sand);
        }

        public static GameObject SpawnNpc(Transform parent, Vector3 at, int variant)
        {
            RailroadMaterials.Ensure();
            Material body = variant % 3 == 0 ? RailroadMaterials.SeatEmpty : (variant % 3 == 1 ? RailroadMaterials.Rock : RailroadMaterials.RockDark);
            Material torso = variant % 2 == 0 ? RailroadMaterials.RockDark : RailroadMaterials.LockedRail;
            Material head = RailroadMaterials.Stonebridge;
            return Spawn("Npc", parent, at, body, torso, head);
        }
    }
}
