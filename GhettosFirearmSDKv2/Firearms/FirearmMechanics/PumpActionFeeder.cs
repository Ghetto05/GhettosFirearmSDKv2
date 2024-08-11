using System.Collections;
using System.Collections.Generic;
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

        private Transform targetTransform;
        private float rotationStartTime;

        private void Start()
        {
            rotationTime = 0.3f;
            targetTransform = idle;
        }

        private void Update()
        {
            if (CartridgesInTrigger())
            {
                if (targetTransform != opened)
                {
                    targetTransform = opened;
                    rotationStartTime = Time.time;
                }
            }
            else
            {
                if (targetTransform != idle)
                {
                    targetTransform = idle;
                    rotationStartTime = Time.time;
                }
            }

            var t = (Time.time - rotationStartTime) / rotationTime;
            t = Mathf.Clamp01(t);

            root.rotation = Quaternion.Slerp(root.rotation, targetTransform.rotation, t);

            if (t >= 1.0f)
            {
                targetTransform = CartridgesInTrigger() ? opened : idle;
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
