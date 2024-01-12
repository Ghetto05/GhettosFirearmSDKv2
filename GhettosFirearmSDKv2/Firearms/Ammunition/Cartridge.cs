using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using UnityEngine.Events;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    public class Cartridge : MonoBehaviour
    {
        public bool disallowDespawn = false;
        public bool keepRotationAtZero;
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

        private void Awake()
        {
            item = GetComponent<Item>();
            if (firedOnlyObject != null) firedOnlyObject.SetActive(false);
            if (unfiredOnlyObject != null) unfiredOnlyObject.SetActive(true);
        }

        private void Update()
        {
            if (keepRotationAtZero && loaded && transform.localEulerAngles != Vector3.zero) transform.localEulerAngles = Vector3.zero;
            if (!disallowDespawn && !loaded && fired && !Mathf.Approximately(FirearmsSettings.cartridgeDespawnTime, 0f)) StartCoroutine(Despawn());
        }

        IEnumerator Despawn()
        {
            do
            {
                yield return new WaitForSeconds(FirearmsSettings.cartridgeDespawnTime);
            } while (loaded || item.physicBody.rigidBody.isKinematic);
            item.Despawn();
        }

        public void Fire(List<Vector3> hits, List<Vector3> directions, Transform muzzle, List<Creature> hitCreatures, bool fire)
        {
            fired = fire;
            if (firedOnlyObject != null) firedOnlyObject.SetActive(true);
            if (unfiredOnlyObject != null) unfiredOnlyObject.SetActive(false);
            ToggleTK(false);
            onFireEvent?.Invoke();
            OnFiredWithHitPointsAndMuzzle?.Invoke(hits, directions, muzzle);
            OnFiredWithHitPointsAndMuzzleAndCreatures?.Invoke(hits, directions, hitCreatures, muzzle);
            if (destroyOnFire) item.Despawn();
        }

        public void Detonate()
        {
            FireMethods.Fire(item, cartridgeFirePoint, data, out List<Vector3> hits, out List<Vector3> trajectories, out List<Creature> hitCreatures, 1f, false);
            if (detonationParticle != null) detonationParticle.Play();
            if (item != null) FireMethods.ApplyRecoil(transform, item.physicBody.rigidBody, data.recoil / 4, 0f, 1f, null);
            Util.PlayRandomAudioSource(detonationSounds);
            Fire(hits, trajectories, cartridgeFirePoint, hitCreatures, true);
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

        public void ToggleTK(bool active)
        {
            foreach (Handle handle in item.handles)
            {
                handle.SetTelekinesis(active);
            }
        }

        public void ToggleHandles(bool active, bool forced = false)
        {
            foreach (Handle handle in item.handles)
            {
                handle.SetTouch(active && (!fired || forced));
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

        public delegate void OnFiredWithCreatures(List<Vector3> hitPoints, List<Vector3> trajectories, List<Creature> hitCreatures, Transform muzzle);
        public event OnFiredWithCreatures OnFiredWithHitPointsAndMuzzleAndCreatures;
    }
}
