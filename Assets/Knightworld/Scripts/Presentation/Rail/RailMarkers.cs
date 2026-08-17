using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class TownMarker : MonoBehaviour
    {
        public string TownId;
    }

    public sealed class ShopMarker : MonoBehaviour
    {
        public string TownId;
    }

    public sealed class DestBeacon : MonoBehaviour
    {
        public float Phase;

        private void Update()
        {
            transform.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.4f + Phase) * 0.14f, 0f);
        }
    }

    public sealed class PassengerMarker : MonoBehaviour
    {
        public int PassengerId;
    }
}
