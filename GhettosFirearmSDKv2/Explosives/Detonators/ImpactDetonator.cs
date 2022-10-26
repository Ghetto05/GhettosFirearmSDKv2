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
        public float minimumArmingDistance;
        public float minimumArmingTime;
        public float minimumImpactForce;
        bool armed = false;

        private float startTime;
        private Vector3 startPoint;
        float distanceTravelled = 0f;
        Vector3 lastPoint;

        private void Awake()
        {
            if (startAtAwake) StartArming();
        }

        void Update()
        {
            distanceTravelled += Vector3.Distance(this.transform.position, lastPoint);
            lastPoint = this.transform.position;
        }

        public void StartArming()
        {
            startTime = Time.time;
            startPoint = this.transform.position;
            armed = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsArmed() && TriggerColliderHit(collision))
            {
                if (explosive != null) explosive.Detonate(delay);
            }
        }

        public bool IsArmed()
        {
            return armed && distanceTravelled >= minimumArmingDistance && Time.time - startTime >= minimumArmingTime;
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
