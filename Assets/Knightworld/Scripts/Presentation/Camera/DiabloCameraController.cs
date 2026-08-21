using UnityEngine;
using UnityEngine.InputSystem;

namespace Knightworld.Presentation
{
    public sealed class DiabloCameraController : MonoBehaviour
    {
        public Transform Target;
        public float Distance = 12f;
        public float MinDistance = 7f;
        public float MaxDistance = 22f;
        public float Pitch = 58f;
        public float Yaw = 45f;
        public float FollowLerp = 12f;
        public float ZoomSpeed = 4f;
        public Vector3 LookOffset = new Vector3(0f, 0.9f, 0f);

        private Vector3 _look;

        public void SnapTo(Transform target)
        {
            Target = target;
            if (target == null)
                return;
            _look = target.position + LookOffset;
            Apply();
        }

        private void LateUpdate()
        {
            if (Target == null)
                return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    Distance = Mathf.Clamp(Distance - scroll * 0.01f * ZoomSpeed, MinDistance, MaxDistance);
            }

            Vector3 want = Target.position + LookOffset;
            _look = Vector3.Lerp(_look, want, 1f - Mathf.Exp(-FollowLerp * Time.deltaTime));
            Apply();
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.SetPositionAndRotation(_look + rotation * (Vector3.back * Distance), rotation);
        }
    }
}
