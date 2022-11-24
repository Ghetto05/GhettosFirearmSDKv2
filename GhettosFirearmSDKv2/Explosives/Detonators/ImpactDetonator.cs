using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GhettosFirearmSDKv2.Explosives;

namespace GhettosFirearmSDKv2
{
    public class ImpactDetonator : MonoBehaviour
    {
        public Explosive explosive;
        public Collider[] triggers;
        public float delay;
        public bool startAtAwake;
        public float minimumArmingTime;
        public float minimumImpactForce;
        bool armed = false;

        private float startTime;

        private void Awake()
        {
            if (startAtAwake) StartArming();
        }

        public void StartArming()
        {
            startTime = Time.time;;
            armed = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsArmed() && TriggerColliderHit(collision))
            {
                if (explosive != null)
                {
                    explosive.Detonate(delay);
                    explosive.impactNormal = collision.contacts[0].normal;
                }
            }
        }

        public bool IsArmed()
        {
            return armed && Time.time - startTime >= minimumArmingTime;
        }

        private bool TriggerColliderHit(Collision collision)
        {
            foreach (Collider c in triggers)
            {
                if (Util.CheckForCollisionWithThisCollider(collision, c)) return true;
            }
            return false;
        }
    }
}
