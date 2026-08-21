using UnityEngine;
using UnityEngine.InputSystem;

namespace Knightworld.Presentation
{
    public sealed class PlatformPlayer : MonoBehaviour
    {
        public float MoveSpeed = 6.5f;
        public float TurnSpeed = 720f;

        private Camera _camera;
        private Bounds _walkBounds;
        private Vector3 _velocity;
        private PlatformTrain _train;
        private PlatformStationDesk _desk;

        public void Initialize(Camera worldCamera, Bounds walkBounds, PlatformTrain train, PlatformStationDesk desk)
        {
            _camera = worldCamera;
            _walkBounds = walkBounds;
            _train = train;
            _desk = desk;
        }

        private void Update()
        {
            if (_desk != null && _desk.IsOpen)
                return;

            Vector2 input = ReadMove();
            Vector3 wish = Vector3.zero;
            if (input.sqrMagnitude > 0.01f)
            {
                input.Normalize();
                float yaw = _camera != null ? _camera.transform.eulerAngles.y : 0f;
                var yawRotation = Quaternion.Euler(0f, yaw, 0f);
                wish = yawRotation * new Vector3(input.x, 0f, input.y);
                wish.y = 0f;
                if (wish.sqrMagnitude > 0.0001f)
                    wish.Normalize();
            }

            _velocity = wish * MoveSpeed;
            if (_velocity.sqrMagnitude > 0.01f)
            {
                var facing = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, TurnSpeed * Time.deltaTime);
            }

            Vector3 next = transform.position + _velocity * Time.deltaTime;
            next.x = Mathf.Clamp(next.x, _walkBounds.min.x, _walkBounds.max.x);
            next.z = Mathf.Clamp(next.z, _walkBounds.min.z, _walkBounds.max.z);
            next.y = _walkBounds.center.y;
            transform.position = next;

            if (_desk != null)
                _desk.Tick(transform.position);
            if (_train != null)
                _train.TryEnter(transform.position);
        }

        private static Vector2 ReadMove()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;
            Vector2 move = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            return move;
        }
    }
}
