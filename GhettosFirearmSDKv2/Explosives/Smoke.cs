using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2.Explosives
{
    public class Smoke : Explosive
    {
        bool active = false;
        bool ready = false;
        public float range;
        public float duration;
        public float emissionDuration;
        public float timestamp;
        public AudioSource loop;
        public ParticleSystem particle;
        CapsuleCollider zone;

        void Awake()
        {
            if (item != null)
            {
                item.disallowDespawn = true;
            }
        }

        public override void ActualDetonate()
        {
            active = true;
            GameObject obj = new GameObject("Smoke_Zone");
            obj.layer = 28;
            zone = obj.AddComponent<CapsuleCollider>();
            zone.isTrigger = true;
            obj.transform.parent = this.transform;
            zone.radius = range;
            obj.transform.localPosition = Vector3.zero;
            loop.Play();
            particle.Play();
            timestamp = Time.time;
            if (gameObject.GetComponentInParent<Rigidbody>() is Rigidbody rb) rb.velocity = Vector3.zero;
            ready = true;
            base.ActualDetonate();
        }

        void Update()
        {
            if (!detonated || !ready) return;

            if (Time.time >= timestamp + emissionDuration)
            {
                loop.Stop();
                zone.gameObject.transform.SetParent(null);
            }

            if (!active) return;

            if (Time.time >= timestamp + duration)
            {
                active = false;
                if (item != null)
                {
                    item.disallowDespawn = false;
                }
            }
        }
    }
}