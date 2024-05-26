using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Bolt assemblies/Muzzle loaded, multi chamber, self cocking")]
    public class MultiChamberMuzzleLoadedBolt : BoltBase, IAmmunitionLoadable
    {
        public bool ejectOnFire;

        public List<AudioSource> ejectSounds;
        public List<AudioSource> insertSounds;

        public List<string> calibers;
        public List<Transform> muzzles;
        public List<Transform> actualMuzzles;
        public List<ParticleSystem> muzzleFlashes;
        public List<ParticleSystem> actualMuzzleFlashes;
        public List<Hammer> hammers;
        public List<Transform> mountPoints;
        public List<Collider> loadColliders;
        public List<Transform> ejectDirections;
        public List<Transform> ejectPoints;
        private Cartridge[] loadedCartridges;
        public List<float> ejectForces;

        private int shotsSinceTriggerReset = 0;
        private bool allowInsert = false;

        private MagazineSaveData data;

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            loadedCartridges = new Cartridge[mountPoints.Count];
            actualMuzzles = muzzles;
            firearm.OnMuzzleCalculatedEvent += Firearm_OnMuzzleCalculatedEvent;
            firearm.OnCollisionEvent += OnCollisionEvent;
            Initialize();
            if (firearm.item.TryGetCustomData(out data))
            {
                for (int i = 0; i < data.contents.Length; i++)
                {
                    if (data.contents[i] != null)
                    {
                        int index = i;
                        Util.SpawnItem(data.contents[index], "Bolt Chamber", ci => { Cartridge c = ci.GetComponent<Cartridge>(); LoadChamber(index, c, false); }, transform.position + Vector3.up * 3);
                    }
                }
                UpdateChamberedRounds();
            }
            else
            {
                firearm.item.AddCustomData(new MagazineSaveData());
                firearm.item.TryGetCustomData(out data);
                data.contents = new string[loadedCartridges.Length];
            }
            allowInsert = true;
        }

        private void Firearm_OnMuzzleCalculatedEvent()
        {
            if (firearm.actualHitscanMuzzle.TryGetComponent(out BreakActionMuzzleOverride overr))
            {
                actualMuzzles = overr.newMuzzles;
                actualMuzzleFlashes = overr.newMuzzleFlashes;
            }
            else
            {
                actualMuzzles = muzzles;
                actualMuzzleFlashes = muzzleFlashes;
            }
        }

        private void OnCollisionEvent(Collision collision)
        {
            if (!allowInsert) return;
            if (collision.collider.GetComponentInParent<Cartridge>() is Cartridge car && !car.loaded)
            {
                foreach (Collider insertCollider in loadColliders)
                {
                    if (Util.CheckForCollisionWithThisCollider(collision, insertCollider))
                    {
                        int index = loadColliders.IndexOf(insertCollider);
                        LoadChamber(index, car);
                    }
                }
            }
        }

        public void LoadChamber(int index, Cartridge cartridge, bool overrideSave = true)
        {
            if (loadedCartridges[index] == null && Util.AllowLoadCartridge(cartridge, calibers[index]))
            {
                if (overrideSave) Util.PlayRandomAudioSource(insertSounds);
                loadedCartridges[index] = cartridge;
                cartridge.item.disallowDespawn = true;
                cartridge.loaded = true;
                cartridge.ToggleHandles(false);
                cartridge.ToggleCollision(false);
                cartridge.UngrabAll();
                Util.IgnoreCollision(cartridge.gameObject, firearm.gameObject, true);
                cartridge.GetComponent<Rigidbody>().isKinematic = true;
                cartridge.transform.parent = mountPoints[index];
                cartridge.transform.localPosition = Vector3.zero;
                cartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
                if (overrideSave) SaveCartridges();
            }
            UpdateChamberedRounds();
        }

        public override void TryEject()
        {
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                TryEjectSingle(i);
            }
        }

        public void TryEjectSingle(int i)
        {
            if (loadedCartridges[i] != null)
            {
                Util.PlayRandomAudioSource(ejectSounds);
                Cartridge c = loadedCartridges[i];
                loadedCartridges[i] = null;
                if (ejectPoints.Count > i && ejectPoints[i] != null)
                {
                    c.transform.position = ejectPoints[i].position;
                    c.transform.rotation = ejectPoints[i].rotation;
                }
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.ToggleCollision(true);
                Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
                Rigidbody rb = c.item.physicBody.rigidBody;
                c.item.disallowDespawn = false;
                c.transform.parent = null;
                c.loaded = false;
                rb.isKinematic = false;
                rb.WakeUp();
                if (ejectDirections[i] != null)
                {
                    AddForceToCartridge(c, ejectDirections[i], ejectForces[i]);
                    //AddTorqueToCartridge(c);
                }
                c.ToggleHandles(true);
                InvokeEjectRound(c);
                SaveCartridges();
            }
        }

        public void FixedUpdate()
        {
            if (firearm.triggerState)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Burst && shotsSinceTriggerReset < firearm.burstSize) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Auto) for (int i = 0; i < mountPoints.Count; i++) { TryFire(); };
            }
            else shotsSinceTriggerReset = 0;
        }

        private void Update()
        {
            BaseUpdate();
        }

        private int GetFirstFireableChamber()
        {
            int car = -1;
            for (int i = loadedCartridges.Length - 1; i >= 0; i--)
            {
                bool hammerCocked = hammers.Count - 1 < i || hammers[i] == null || hammers[i].cocked;
                bool cartridge = loadedCartridges[i] != null && !loadedCartridges[i].fired;
                if (hammerCocked && cartridge) car = i;
            }
            return car;
        }

        public override void TryFire()
        {
            if (state != BoltState.Locked)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            int ca = GetFirstFireableChamber();
            if (ca == -1)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }

            shotsSinceTriggerReset++;
            if (hammers.Count > ca && hammers[ca] != null)
                hammers[ca].Fire();
            foreach (RagdollHand hand in firearm.item.handlers)
            {
                if (hand.playerHand != null || hand.playerHand.controlHand != null)
                    hand.playerHand.controlHand.HapticShort(50f);
            }
            Transform muzzle = muzzles.Count < 2 ? firearm.actualHitscanMuzzle : actualMuzzles[ca];
            Cartridge loadedCartridge = loadedCartridges[ca];
            IncrementBreachSmokeTime();
            firearm.PlayFireSound(loadedCartridge);
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
            {
                if (actualMuzzleFlashes != null && actualMuzzleFlashes.Count > ca && actualMuzzleFlashes[ca] != null && muzzles.Count > 1)
                    actualMuzzleFlashes[ca].Play(); 
                else
                    firearm.PlayMuzzleFlash(loadedCartridge);
            }
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            FireMethods.Fire(firearm.item, muzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
            loadedCartridge.Fire(hits, trajectories, muzzle, hitCreatures, !FirearmsSettings.infiniteAmmo);
            InvokeFireEvent();
            InvokeFireLogicFinishedEvent();
        }

        public override void TryRelease(bool forced = false)
        {
            TryEject();
        }

        public override void Initialize()
        {
        }

        public override void EjectRound()
        {
            if (mountPoints.Count == 1)
                TryEjectSingle(0);
            else if (GetFirstFireableChamber() != -1)
                TryEjectSingle(GetFirstFireableChamber() - 1);
            else if (mountPoints.Count > 0)
                TryEjectSingle(mountPoints.Count - 1);
        }

        public void SaveCartridges()
        {
            data.contents = new string[loadedCartridges.Length];
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                data.contents[i] = loadedCartridges[i]?.item.itemId;
            }
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
            for (int i = 0; i < mountPoints.Count; i++)
            {
                if (loadedCartridges[i] != null)
                {
                    loadedCartridges[i].GetComponent<Rigidbody>().isKinematic = true;
                    loadedCartridges[i].transform.parent = mountPoints[i];
                    loadedCartridges[i].transform.localPosition = Vector3.zero;
                    loadedCartridges[i].transform.localEulerAngles = Util.RandomCartridgeRotation();
                }
            }
        }

        private int GetFirstFreeChamber()
        {
            List<int> availableChambers = new List<int>();
            for (int i = loadedCartridges.Length - 1; i >= 0; i--)
            {
                if (loadedCartridges[i] == null)
                    availableChambers.Add(i);
            }

            if (availableChambers.Count == 0)
                return 0;
            
            return availableChambers.First();
        }

        public string GetCaliber()
        {
            return calibers[GetFirstFreeChamber()];
        }

        public int GetCapacity()
        {
            return loadedCartridges.Length;
        }

        public List<Cartridge> GetLoadedCartridges()
        {
            return loadedCartridges.ToList();
        }

        public void LoadRound(Cartridge cartridge)
        {
            LoadChamber(GetFirstFreeChamber(), cartridge);
        }

        public void ClearRounds()
        {
            foreach (Cartridge cartridge in loadedCartridges)
            {
                EjectRound();
                cartridge.item.Despawn(0.05f);
            }
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public bool GetForceCorrectCaliber()
        {
            return false;
        }

        public List<string> GetAlternativeCalibers()
        {
            return null;
        }
    }
}