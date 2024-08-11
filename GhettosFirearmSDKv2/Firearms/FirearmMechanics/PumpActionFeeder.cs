using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PumpActionFeeder : MonoBehaviour
    {
        public Collider triggerCollider;
        public Transform root;
        public Transform idle;
        public Transform opened;
        public float rotationTime = 1.0f;
        public bool openingEditorOverride;

        private Transform _targetTransform;
        private float _rotationStartTime;

        private void Start()
        {
            rotationTime = 0.3f;
            _targetTransform = idle;
        }

        private void Update()
        {
            if (CartridgesInTrigger())
            {
                if (_targetTransform != opened)
                {
                    _targetTransform = opened;
                    _rotationStartTime = Time.time;
                }
            }
            else
            {
                if (_targetTransform != idle)
                {
                    _targetTransform = idle;
                    _rotationStartTime = Time.time;
                }
            }

            var t = (Time.time - _rotationStartTime) / rotationTime;
            t = Mathf.Clamp01(t);

            root.rotation = Quaternion.Slerp(root.rotation, _targetTransform.rotation, t);

            if (t >= 1.0f)
            {
                _targetTransform = CartridgesInTrigger() ? opened : idle;
            }
        }

        public bool CartridgesInTrigger()
        {
            if (openingEditorOverride) return true;
            var bounds = triggerCollider.bounds;
            var triggerCenter = bounds.center;
            var triggerSize = bounds.size;
            var colliders = Physics.OverlapBox(triggerCenter, triggerSize * 0.5f, Quaternion.identity);
            foreach (var collider in colliders)
            {
                var cartridgeScript = collider.GetComponentInParent<Cartridge>();
                if (cartridgeScript != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
