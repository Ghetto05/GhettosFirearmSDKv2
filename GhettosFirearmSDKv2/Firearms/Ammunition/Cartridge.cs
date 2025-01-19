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
        public bool forceRotation;
        public float forceRotationIncrement;
        public GameObject firedOnlyObject;
        public GameObject unfiredOnlyObject;
        public string caliber;
        public bool destroyOnFire;
        public ProjectileData data;
        public bool loaded;
        public Item item;
        public ParticleSystem detonationParticle;
        public AudioSource[] detonationSounds;
        public ParticleSystem additionalMuzzleFlash;
        public List<Collider> colliders;
        public Transform cartridgeFirePoint;
        public UnityEvent onFireEvent;
        
        public CartridgeSaveData SaveData;

        private bool _fired;
        public bool Fired
        {
            get
            {
                return _fired;
            }
            set
            {
                _fired = value;
                if (SaveData != null)
                    SaveData.IsFired = value;
            }
        }

        private void Awake()
        {
            item = GetComponent<Item>();
        }
        
        private void Start()
        {
            if (unfiredOnlyObject && !Fired)
                unfiredOnlyObject.SetActive(true);
            Invoke(nameof(InvokedStart), Settings.invokeTime);
            if (data.isInert)
                Fired = true;
            if (Settings.disableCartridgeImpactSounds)
                foreach (var c in colliders)
                {
                    c.material = null;
                }
        }

        private void InvokedStart()
        {
            if (firedOnlyObject)
            {
                firedOnlyObject.SetActive(Fired);
                var ren = firedOnlyObject.GetComponentsInChildren<Renderer>(true);
                if (ren.Length > 0)
                {
                    item.renderers.AddRange(ren);
                    item.lightVolumeReceiver.SetRenderers(item.renderers);
                }
            }

            if (item.TryGetCustomData(out SaveData))
            {
                if (SaveData.IsFired && !Fired)
                    SetFired();
            }
            else
            {
                SaveData = new CartridgeSaveData(item.itemId, Fired);
                item.AddCustomData(SaveData);
            }
        }

        private void Update()
        {
            if (loaded)
            {
                if (keepRotationAtZero && transform.localEulerAngles != Vector3.zero)
                    transform.localEulerAngles = Vector3.zero;
                if (forceRotation && transform.localEulerAngles.z % forceRotationIncrement > 0.001f)
                    SnapToNearestRotation();
            }
            
            if (!disallowDespawn && !loaded && Fired && !Mathf.Approximately(Settings.cartridgeDespawnTime, 0f))
                StartCoroutine(Despawn());
        }
        
        public void SnapToNearestRotation()
        {
            var currentZ = transform.eulerAngles.z;
            currentZ = currentZ % 360;
            if (currentZ < 0)
                currentZ += 360;
            var nearestSnap = Mathf.Round(currentZ / forceRotationIncrement) * forceRotationIncrement;
            transform.rotation = Quaternion.Euler(0, 0, nearestSnap);
        }

        private IEnumerator Despawn()
        {
            do
            {
                yield return new WaitForSeconds(Settings.cartridgeDespawnTime);
            } while (loaded || item.physicBody.rigidBody.isKinematic || item.DisallowDespawn);
            item.Despawn();
        }

        public void DisableCartridge(bool fire = true)
        {
            Fired = fire;
            ToggleTk(fire);
        }

        public void Fire(List<Vector3> hits, List<Vector3> directions, Transform muzzle, List<Creature> hitCreatures, List<Creature> killedCreatures, bool fire)
        {
            DisableCartridge(fire);
            if (firedOnlyObject != null)
                firedOnlyObject.SetActive(true);
            if (unfiredOnlyObject != null)
                unfiredOnlyObject.SetActive(false);
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

        public void SetFired()
        {
            Fired = true;
            if (firedOnlyObject != null)
                firedOnlyObject.SetActive(true);
            if (unfiredOnlyObject != null)
                unfiredOnlyObject.SetActive(false);
            ToggleTk(false);
            if (destroyOnFire)
                item.Despawn();
        }

        public void Detonate()
        {
            FireMethods.Fire(item, cartridgeFirePoint, data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, 1f, false);
            if (detonationParticle != null)
                detonationParticle.Play();
            if (item != null)
                FireMethods.ApplyRecoil(transform, item, data.recoil / 4, 0f, 1f, null);
            Util.PlayRandomAudioSource(detonationSounds);
            Fire(hits, trajectories, cartridgeFirePoint, hitCreatures, killedCreatures, true);
        }

        public void Reset()
        {
            Fired = false;
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
                handle.SetTouch(active && (!Fired || forced));
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
