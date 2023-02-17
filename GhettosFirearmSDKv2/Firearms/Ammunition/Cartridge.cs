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
        public UnityEvent onFireEvent;
        private List<Renderer> renderers;
        private Item currentRendererSource;

        private void Awake()
        {
            item = this.GetComponent<Item>();
            if (firedOnlyObject != null) firedOnlyObject.SetActive(false);
            if (unfiredOnlyObject != null) unfiredOnlyObject.SetActive(true);
            renderers = GetComponentsInChildren<Renderer>();
            currentRendererSource = item;
        }

        public void SetRenderersTo(Item newItem)
        {
            foreach (Renderer r in renderers)
            {
                currentRendererSource.renderers.Remove(r);
            }
            currentRendererSource.lightVolumeReceiver.SetRenderers(currentRendererSource.renderers);
            newItem.renderers.AddRange(renderers);
            newItem.lightVolumeReceiver.SetRenderers(newItem.renderers);
            currentRendererSource = newItem;
        }

        public void Fire(List<Vector3> hits, List<Vector3> directions, Transform muzzle)
        {
            fired = true;
            if (firedOnlyObject != null) firedOnlyObject.SetActive(true);
            if (unfiredOnlyObject != null) unfiredOnlyObject.SetActive(false);
            onFireEvent?.Invoke();
            OnFiredWithHitPointsAndMuzzle?.Invoke(hits, directions, muzzle);
            if (destroyOnFire) item.Despawn();
        }

        public void Detonate()
        {
            FireMethods.Fire(item, cartridgeFirePoint, data, out List<Vector3> hits, out List<Vector3> trajectories, 1f);
            if (detonationParticle != null) detonationParticle.Play();
            FireMethods.ApplyRecoil(this.transform, this.item.rb, data.recoil, 0f, 1f, null);
            Util.PlayRandomAudioSource(detonationSounds);
            Fire(hits, trajectories, null);
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

        public delegate void OnFired(List<Vector3> hitPoints, List<Vector3> trajectories, Transform muzzle);
        public event OnFired OnFiredWithHitPointsAndMuzzle;
    }
}
