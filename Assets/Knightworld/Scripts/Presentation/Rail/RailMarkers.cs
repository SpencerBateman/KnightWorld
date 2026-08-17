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

    public sealed class PassengerMarker : MonoBehaviour
    {
        public int PassengerId;
    }
}
