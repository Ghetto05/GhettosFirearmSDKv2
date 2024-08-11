using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Ammunition/Sticky bomb")]
    [RequireComponent(typeof(Item))]
    public class StickyBomb : MonoBehaviour
    {
        public List<Collider> colliders;
        public Item item;
        public UnityEvent onStickEvent;
        private bool _stuck;

        public void Awake()
        {
            item = GetComponent<Item>();
            item.mainCollisionHandler.OnCollisionStartEvent += MainCollisionHandler_OnCollisionStartEvent;
        }

        private void MainCollisionHandler_OnCollisionStartEvent(CollisionInstance collisionInstance)
        {
            if (colliders.Count > 0)
            {
                if (colliders.Contains(collisionInstance.sourceCollider))
                {
                    StickTo(collisionInstance.targetCollider.attachedRigidbody);
                }
            }
            else
            {
                StickTo(collisionInstance.targetCollider.attachedRigidbody);
            }
        }

        public void StickTo(Rigidbody rb)
        {
            if (_stuck)
                return;
            _stuck = true;
            item.DisallowDespawn = true;
            var joint = item.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = rb;
            onStickEvent?.Invoke();
        }
    }
}
