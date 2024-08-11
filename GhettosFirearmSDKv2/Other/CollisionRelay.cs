using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class CollisionRelay : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            OnCollisionEnterEvent?.Invoke(collision);
        }

        public delegate void OnCollisionEnterDelegate(Collision collision);
        public event OnCollisionEnterDelegate OnCollisionEnterEvent;
    }
}