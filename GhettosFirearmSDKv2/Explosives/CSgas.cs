using GhettosFirearmSDKv2.Chemicals;
using UnityEngine;

namespace GhettosFirearmSDKv2.Explosives
{
    public class CSgas : Explosive
    {
        private bool _active;
        private bool _ready;
        public float range;
        public float duration;
        public float emissionDuration;
        public float timestamp;
        public AudioSource loop;
        public ParticleSystem particle;
        public GameObject volume;
        private CapsuleCollider _zone;
        private GameObject _zoneObj;

        private void Awake()
        {
            if (item != null)
            {
                item.DisallowDespawn = true;
            }
        }

        public override void ActualDetonate()
        {
            _active = true;
            _zoneObj = new GameObject("CSgas_Zone");
            _zoneObj.layer = LayerMask.NameToLayer("Zone");
            _zone = _zoneObj.AddComponent<CapsuleCollider>();
            _zone.isTrigger = true;
            _zoneObj.transform.parent = transform;
            _zone.radius = range;
            _zoneObj.transform.localPosition = Vector3.zero;
            loop?.Play();
            particle?.Play();
            timestamp = Time.time;
            if (gameObject.GetComponentInParent<Rigidbody>() is { } rb) rb.velocity = Vector3.zero;
            _ready = true;
            base.ActualDetonate();
        }

        private void Update()
        {
            if (!detonated || !_ready) return;

            if (Time.time >= timestamp + emissionDuration && loop.isPlaying)
            {
                loop?.Stop();
            }

            if (!_active) return;
            volume?.SetActive(!PlayerEffectsAndChemicalsModule.local.WearingGasMask());

            if (Time.time >= timestamp + duration)
            {
                _active = false;
                Destroy(_zoneObj);
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