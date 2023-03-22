using System.Collections;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class CollisionRelay : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            onCollisionEnterEvent?.Invoke(collision);
        }

        public delegate void OnCollisionEnterDelegate(Collision collision);
        public event OnCollisionEnterDelegate onCollisionEnterEvent;
    }
}