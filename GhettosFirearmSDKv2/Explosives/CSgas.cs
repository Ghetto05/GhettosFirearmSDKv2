using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.Rendering;
using GhettosFirearmSDKv2.Chemicals;

namespace GhettosFirearmSDKv2.Explosives
{
    public class CSgas : Explosive
    {
        bool active = false;
        bool ready = false;
        public float range;
        public float duration;
        public float emissionDuration;
        public float timestamp;
        public AudioSource loop;
        public ParticleSystem particle;
        public GameObject volume;
        CapsuleCollider zone;
        GameObject zoneObj;

        void Awake()
        {
            if (item != null)
            {
                item.DisallowDespawn = true;
            }
        }

        public override void ActualDetonate()
        {
            active = true;
            zoneObj = new GameObject("CSgas_Zone");
            zoneObj.layer = LayerMask.NameToLayer("Zone");
            zone = zoneObj.AddComponent<CapsuleCollider>();
            zone.isTrigger = true;
            zoneObj.transform.parent = transform;
            zone.radius = range;
            zoneObj.transform.localPosition = Vector3.zero;
            loop?.Play();
            particle?.Play();
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
                loop?.Stop();
            }

            if (!active) return;
            volume?.SetActive(!PlayerEffectsAndChemicalsModule.local.WearingGasMask());

            if (Time.time >= timestamp + duration)
            {
                active = false;
                Destroy(zoneObj);
                volume?.SetActive(false);
                if (item != null)
                {
                    item.DisallowDespawn = false;
                }
                else Destroy(gameObject);
            }
        }
    }
}