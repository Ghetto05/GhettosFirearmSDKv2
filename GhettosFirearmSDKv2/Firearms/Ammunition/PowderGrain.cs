using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PowderGrain : MonoBehaviour
    {
        private void OnCollisionEnter(Collision other)
        {
            if (other.collider.GetComponentInParent<PowderReceiver>())
            {
                PowderReceiver receiver = other.collider.GetComponentInParent<PowderReceiver>();
                if (receiver.currentAmount >= receiver.grainCapacity || receiver.blocked || other.collider != receiver.loadCollider)
                    return;
                receiver.currentAmount++;
            }
            Destroy(gameObject);
        }
    }
}
