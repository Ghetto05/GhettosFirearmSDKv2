using System.Collections;
using System.Collections.Generic;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class Cartridge : MonoBehaviour
    {
        public bool disallowDespawn;
        public bool keepRotationAtZero;
        public GameObject firedOnlyObject;
        public GameObject unfiredOnlyObject;
        public string caliber;
        public bool destroyOnFire;
        public ProjectileData data;
        public bool fired;
        public bool loaded;
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
        }
        
        private void Start()
        {
            if (unfiredOnlyObject != null)
                unfiredOnlyObject.SetActive(true);
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        private void InvokedStart()
        {
            if (firedOnlyObject != null)
            {
                firedOnlyObject.SetActive(false);
                var ren = firedOnlyObject.GetComponentsInChildren<Renderer>(true);
                if (ren.Length > 0)
                {
                    item.renderers.AddRange(ren);
                    item.lightVolumeReceiver.SetRenderers(item.renderers);
                }
            }
        }

        private void Update()
        {
            if (keepRotationAtZero && loaded && transform.localEulerAngles != Vector3.zero)
                transform.localEulerAngles = Vector3.zero;
            if (!disallowDespawn && !loaded && fired && !Mathf.Approximately(Settings.cartridgeDespawnTime, 0f))
                StartCoroutine(Despawn());
        }

        private IEnumerator Despawn()
        {
            do
            {
                yield return new WaitForSeconds(Settings.cartridgeDespawnTime);
            } while (loaded || item.physicBody.rigidBody.isKinematic || item.DisallowDespawn);
            item.Despawn();
        }

        public void Fire(List<Vector3> hits, List<Vector3> directions, Transform muzzle, List<Creature> hitCreatures, List<Creature> killedCreatures, bool fire)
        {
            fired = fire;
            if (firedOnlyObject != null)
                firedOnlyObject.SetActive(true);
            if (unfiredOnlyObject != null)
                unfiredOnlyObject.SetActive(false);
            ToggleTk(false);
            onFireEvent?.Invoke();
            OnFiredWithHitPointsAndMuzzle?.Invoke(hits, directions, muzzle);
            OnFiredWithHitPointsAndMuzzleAndCreatures?.Invoke(hits, directions, hitCreatures, muzzle, killedCreatures);
            if (additionalMuzzleFlash != null)
            {
                var additionalMuzzleFlashInstance = Instantiate(additionalMuzzleFlash.gameObject, muzzle, true);
                additionalMuzzleFlashInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                additionalMuzzleFlashInstance.GetComponent<ParticleSystem>().Play();
                var main = additionalMuzzleFlash.main;
                StartCoroutine(Explosive.DelayedDestroy(additionalMuzzleFlashInstance, main.duration + main.startLifetime.constantMax * 4));
            }
            if (destroyOnFire && fire)
                item.Despawn();
        }

        public void Detonate()
        {
            FireMethods.Fire(item, cartridgeFirePoint, data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, 1f, false);
            if (detonationParticle != null)
                detonationParticle.Play();
            if (item != null)
                FireMethods.ApplyRecoil(transform, item.physicBody.rigidBody, data.recoil / 4, 0f, 1f, null);
            Util.PlayRandomAudioSource(detonationSounds);
            Fire(hits, trajectories, cartridgeFirePoint, hitCreatures, killedCreatures, true);
        }

        public void Reset()
        {
            fired = false;
            if (firedOnlyObject != null) firedOnlyObject.SetActive(false);
            if (unfiredOnlyObject != null) unfiredOnlyObject.SetActive(true);
        }

        public void UngrabAll()
        {
            var hands = item.handlers.ToArray();
            foreach (var hand in hands)
            {
                hand.UnGrab(false);
            }
        }

        public void ToggleTk(bool active)
        {
            foreach (var handle in item.handles)
            {
                handle.SetTelekinesis(active);
            }
        }

        public void ToggleHandles(bool active, bool forced = false)
        {
            foreach (var handle in item.handles)
            {
                handle.SetTouch(active && (!fired || forced));
            }
        }

        public void ToggleCollision(bool active)
        {
            foreach (var c in colliders)
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

        public delegate void OnFiredWithCreatures(List<Vector3> hitPoints, List<Vector3> trajectories, List<Creature> hitCreatures, Transform muzzle, List<Creature> killedCreatures);
        public event OnFiredWithCreatures OnFiredWithHitPointsAndMuzzleAndCreatures;
    }
}
