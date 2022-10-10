using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class CollisionDisabler : MonoBehaviour
    {
        public List<Collider> obj1Colliders;
        public List<Collider> obj2Colliders;

        void Start()
        {
            foreach (Collider c1 in obj1Colliders)
            {
                foreach (Collider c2 in obj2Colliders)
                {
                    Physics.IgnoreCollision(c1, c2, true);
                }
            }
        }
    }
}
