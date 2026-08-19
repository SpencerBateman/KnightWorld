using UnityEngine;
using UnityEngine.InputSystem;

namespace Knightworld.Presentation
{
    public sealed class IsoCameraController : MonoBehaviour
    {
        public Vector3 LookTarget;
        public float Distance = 16f;
        public float MinDistance = 8f;
        public float MaxDistance = 28f;
        public float Pitch = 50f;
        public float Yaw = 45f;
        public float PanSpeed = 10f;
        public float ZoomSpeed = 4f;
        public float RotateSpeed = 180f;
        public float FollowLerp = 6f;
        public bool InputLocked;
        public bool IgnorePan;

        private float _targetYaw;
        private Vector3 _followTarget;
        private float _savedDistance;

        public void FrameRoute(Vector3 from, Vector3 to)
        {
            InputLocked = true;
            _savedDistance = Distance;
            float span = Vector3.Distance(from, to);
            float framed = Mathf.Clamp(span * 1.4f + 14f, 22f, 110f);
            Distance = framed * 0.25f;
            FocusImmediate(from);
        }

        public void UnlockOn(Vector3 target)
        {
            InputLocked = false;
            Distance = _savedDistance > 0.1f ? _savedDistance : Distance;
            FocusImmediate(target);
        }

        public void FocusImmediate(Vector3 target)
        {
            LookTarget = target;
            _followTarget = target;
            _targetYaw = Yaw;
            Apply();
        }

        public void Follow(Vector3 target)
        {
            _followTarget = target;
        }

        private void Awake()
        {
            _targetYaw = Yaw;
            _followTarget = LookTarget;
        }

        private void Update()
        {
            if (InputLocked)
            {
                LookTarget = Vector3.Lerp(LookTarget, _followTarget, 1f - Mathf.Exp(-FollowLerp * Time.deltaTime));
                Apply();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (!IgnorePan)
                {
                    Vector2 move = Vector2.zero;
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
                    if (move.sqrMagnitude > 0f)
                    {
                        move.Normalize();
                        var yawRotation = Quaternion.Euler(0f, Yaw, 0f);
                        var right = yawRotation * Vector3.right;
                        var forward = yawRotation * Vector3.forward;
                        _followTarget += (right * move.x + forward * move.y) * (PanSpeed * Time.deltaTime);
                    }
                }

                if (keyboard.qKey.wasPressedThisFrame)
                    _targetYaw -= 90f;
                if (keyboard.eKey.wasPressedThisFrame)
                    _targetYaw += 90f;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    Distance = Mathf.Clamp(Distance - scroll * 0.01f * ZoomSpeed, MinDistance, MaxDistance);
            }

            LookTarget = Vector3.Lerp(LookTarget, _followTarget, 1f - Mathf.Exp(-FollowLerp * Time.deltaTime));
            Yaw = Mathf.MoveTowardsAngle(Yaw, _targetYaw, RotateSpeed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.SetPositionAndRotation(LookTarget + rotation * (Vector3.back * Distance), rotation);
        }
    }
}
