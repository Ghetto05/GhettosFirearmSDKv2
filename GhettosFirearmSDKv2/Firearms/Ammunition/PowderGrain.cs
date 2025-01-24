using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PowderGrain : MonoBehaviour
    {
        private void OnCollisionEnter(Collision other)
        {
            if (other.collider.GetComponentInParent<PowderReceiver>())
            {
                var receiver = other.collider.GetComponentInParent<PowderReceiver>();
                if (receiver.currentAmount < receiver.grainCapacity && !receiver.blocked && other.collider == receiver.loadCollider)
                    receiver.currentAmount += 2;
                Destroy(gameObject);
            }
        }
    }
}
