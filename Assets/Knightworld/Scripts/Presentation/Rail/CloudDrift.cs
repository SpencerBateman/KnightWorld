using UnityEngine;

namespace Knightworld.Presentation
{
    public sealed class CloudDrift : MonoBehaviour
    {
        public Vector3 Wind = new Vector3(1.1f, 0f, 0.18f);
        public Vector3 Min;
        public Vector3 Max;
        public float BobAmp = 0.22f;
        public float BobSpeed = 0.32f;
        public float Phase;

        private float _baseY;

        private void Awake()
        {
            _baseY = transform.position.y;
        }

        private void Update()
        {
            Vector3 p = transform.position;
            p += Wind * Time.deltaTime;
            if (p.x > Max.x)
                p.x = Min.x + (p.x - Max.x);
            if (p.x < Min.x)
                p.x = Max.x - (Min.x - p.x);
            if (p.z > Max.z)
                p.z = Min.z + (p.z - Max.z);
            if (p.z < Min.z)
                p.z = Max.z - (Min.z - p.z);
            p.y = _baseY + Mathf.Sin(Time.time * BobSpeed + Phase) * BobAmp;
            transform.position = p;
        }
    }

    public sealed class WaterSheen : MonoBehaviour
    {
        public float Speed = 0.55f;
        public float Amount = 0.018f;
        public float Phase;

        private Vector3 _scale;

        private void Awake()
        {
            _scale = transform.localScale;
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * Speed + Phase) * Amount;
            transform.localScale = new Vector3(_scale.x * pulse, _scale.y, _scale.z * pulse);
        }
    }
}
