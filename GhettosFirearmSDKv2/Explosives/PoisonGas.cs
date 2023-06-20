using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2.Explosives
{
    public class PoisonGas : Explosive
    {
        bool active = false;
        bool ready = false;
        public float range;
        public float duration;
        public float emissionDuration;
        public float timestamp;
        public float damagePerSecond;
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
            GameObject obj = new GameObject("PoisonGas_Zone");
            GameObject amObj = new GameObject(damagePerSecond.ToString());
            amObj.transform.SetParent(obj.transform);
            amObj.transform.localPosition = Vector3.zero;
            obj.layer = 28;
            zone = obj.AddComponent<CapsuleCollider>();
            zone.isTrigger = true;
            obj.transform.parent = transform;
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

            if (Time.time >= timestamp + emissionDuration && loop.isPlaying)
            {
                loop.Stop();
                zone.gameObject.transform.SetParent(null);
            }

            if (!active) return;

            if (Time.time >= timestamp + duration)
            {
                active = false;
                Destroy(zone.gameObject);
                if (item != null)
                {
                    item.disallowDespawn = false;
                }
                else Destroy(gameObject);
            }
        }
    }
}