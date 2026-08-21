using UnityEngine;
using UnityEngine.SceneManagement;

namespace Knightworld.Presentation
{
    public sealed class PlatformTrain : MonoBehaviour
    {
        private Bounds _boardingZone;
        private bool _loading;

        public void Initialize(Bounds boardingZone)
        {
            _boardingZone = boardingZone;
        }

        public void TryEnter(Vector3 playerPosition)
        {
            if (_loading || !_boardingZone.Contains(playerPosition))
                return;
            _loading = true;
            RailroadController.StartDeparted = true;
            SceneManager.LoadScene(RailroadController.SceneName);
        }
    }
}
