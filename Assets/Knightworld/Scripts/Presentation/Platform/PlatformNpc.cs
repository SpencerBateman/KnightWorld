using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class PlatformNpc : MonoBehaviour
    {
        public float MoveSpeed = 2.8f;
        public float TurnSpeed = 480f;

        private float _laneX;
        private float _y;
        private float _minZ;
        private float _maxZ;
        private float _direction;
        private bool _wrap;

        public void Initialize(float laneX, float y, float minZ, float maxZ, float direction, float speed, bool wrap)
        {
            _laneX = laneX;
            _y = y;
            _minZ = minZ;
            _maxZ = maxZ;
            _direction = direction >= 0f ? 1f : -1f;
            MoveSpeed = speed;
            _wrap = wrap;
            FaceDirection();
        }

        private void Update()
        {
            Vector3 pos = transform.position;
            pos.x = _laneX;
            pos.y = _y;
            pos.z += _direction * MoveSpeed * Time.deltaTime;

            if (_wrap)
            {
                if (_direction > 0f && pos.z > _maxZ)
                    pos.z = _minZ;
                else if (_direction < 0f && pos.z < _minZ)
                    pos.z = _maxZ;
            }
            else
            {
                if (pos.z >= _maxZ)
                {
                    pos.z = _maxZ;
                    _direction = -1f;
                    FaceDirection();
                }
                else if (pos.z <= _minZ)
                {
                    pos.z = _minZ;
                    _direction = 1f;
                    FaceDirection();
                }
            }

            transform.position = pos;
            var facing = Quaternion.LookRotation(new Vector3(0f, 0f, _direction), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, TurnSpeed * Time.deltaTime);
        }

        private void FaceDirection()
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, _direction), Vector3.up);
        }
    }
}
