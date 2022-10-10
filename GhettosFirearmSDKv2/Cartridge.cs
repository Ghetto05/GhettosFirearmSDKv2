using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class Cartridge : MonoBehaviour
    {
        public GameObject firedOnlyObject;
        public GameObject unfiredOnlyObject;
        public string caliber;
        public bool destroyOnFire;
        public ProjectileData data;
        public bool fired = false;
        public bool loaded = false;
        public Item item;
        public ParticleSystem detonationParticle;
        public AudioSource[] detonationSounds;
        public ParticleSystem additionalMuzzleFlash;
        public List<Collider> colliders;
        public Transform cartridgeFirePoint;
        public List<UnityEvent> onFireEvents;

        private void Awake()
        {
            item = this.GetComponent<Item>();
            if (firedOnlyObject != null) firedOnlyObject.SetActive(false);
            if (unfiredOnlyObject != null) unfiredOnlyObject.SetActive(true);
        }

        public void Fire()
        {
            fired = true;
            if (firedOnlyObject != null) firedOnlyObject.SetActive(true);
            if (unfiredOnlyObject != null) unfiredOnlyObject.SetActive(false);
            foreach (UnityEvent ue in onFireEvents)
            {
                ue.Invoke();
            }
            if (destroyOnFire) item.Despawn();
        }

        public void Detonate()
        {
            FireMethods.Fire(item, cartridgeFirePoint, data);
            if (detonationParticle != null) detonationParticle.Play();
            FireMethods.ApplyRecoil(this.transform, this.item.rb, 1f, data.recoil, 0f);
            Util.PlayRandomAudioSource(detonationSounds);
            Fire();
        }

        public void Reset()
        {
            fired = false;
            if (firedOnlyObject != null) firedOnlyObject.SetActive(false);
            if (unfiredOnlyObject != null) unfiredOnlyObject.SetActive(true);
        }

        public void UngrabAll()
        {
            RagdollHand[] hands = item.handlers.ToArray();
            foreach (RagdollHand hand in hands)
            {
                hand.UnGrab(false);
            }
        }

        public void ToggleHandles(bool active)
        {
            foreach (Handle handle in item.handles)
            {
                handle.SetTouch(active);
            }
        }

        public void ToggleCollision(bool active)
        {
            foreach (Collider c in colliders)
            {
                c.enabled = active;
            }
        }

        public void DisableCull()
        {
            item.SetCull(false);
            item.Hide(false);
        }
    }
}
