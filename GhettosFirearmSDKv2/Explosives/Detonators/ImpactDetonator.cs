using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GhettosFirearmSDKv2.Explosives;

namespace GhettosFirearmSDKv2
{
    public class ImpactDetonator : Explosive
    {
        public Explosive explosive;
        public Collider[] triggers;
        public float delay;
        public bool startAtAwake;
        public float minimumArmingTime;
        public float minimumImpactForce;
        public float selfDestructDelay;
        bool armed = false;
        Vector3 startPoint;
        float clearance = 0.3f;

        private float startTime;

        private void Awake()
        {
            if (startAtAwake) StartArming();
            startPoint = transform.position;
        }

        public override void ActualDetonate()
        {
            StartArming();
            base.ActualDetonate();
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
            return armed && Time.time - startTime >= minimumArmingTime && Vector3.Distance(startPoint, transform.position) > clearance;
        }

        private bool TriggerColliderHit(Collision collision)
        {
            foreach (Collider c in triggers)
            {
                if (Util.CheckForCollisionWithThisCollider(collision, c)) return true;
            }
            return false;
        }

        private void Update()
        {
            if (selfDestructDelay > 0.2f && Time.time > startTime + selfDestructDelay)
            {
                explosive.Detonate(delay);
                explosive.impactNormal = this.transform.forward;
            }
        }
    }
}
