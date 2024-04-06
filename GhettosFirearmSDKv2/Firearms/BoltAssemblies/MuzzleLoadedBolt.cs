using System;
using System.Collections;
using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;

namespace GhettosFirearmSDKv2
{
    public class MuzzleLoadedBolt : BoltBase
    {
        public bool ejectCasingOnReleaseButton = true;
        public Cartridge loadedCartridge;
        public Transform roundMount;
        public AudioSource[] ejectSounds;

        public Transform roundEjectPoint;
        public float roundEjectForce;
        public Transform roundEjectDir;
        public bool ejectOnFire;
        int shotsSinceTriggerReset = 0;
        public List<Lock> locks;

        public Hammer hammer;

        public override bool LoadChamber(Cartridge c, bool forced)
        {
            if (loadedCartridge == null && !c.loaded)
            {
                loadedCartridge = c;
                c.item.disallowDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = roundMount;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                SaveChamber(c.item.itemId);
                return true;
            }
            return false;
        }

        public override void TryRelease(bool forced = false)
        {
            if (ejectCasingOnReleaseButton) EjectRound();
        }

        public override void TryFire()
        {
            shotsSinceTriggerReset++;
            if (!Util.AllLocksUnlocked(locks))
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            if (hammer != null)
            {
                bool f = hammer.cocked;
                hammer.Fire();
                if (!f)
                {
                    InvokeFireLogicFinishedEvent();
                    return;
                }
            }

            if (loadedCartridge == null || loadedCartridge.fired)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            foreach (RagdollHand hand in firearm.item.handlers)
            {
                if (hand.playerHand != null && hand.playerHand.controlHand != null)
                    hand.playerHand.controlHand.HapticShort(50f);
            }
            IncrementBreachSmokeTime();
            firearm.PlayFireSound(loadedCartridge);
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                firearm.PlayMuzzleFlash(loadedCartridge);
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, !FirearmsSettings.infiniteAmmo);
            if (ejectOnFire && !FirearmsSettings.infiniteAmmo)
                EjectRound();
            InvokeFireEvent();
            InvokeFireLogicFinishedEvent();
        }

        public override Cartridge GetChamber()
        {
            return loadedCartridge;
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
            if (loadedCartridge == null) return;
            loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
            loadedCartridge.transform.parent = roundMount;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (fireOnTriggerPress && isPulled && firearm.fireMode != FirearmBase.FireModes.Safe)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Burst && shotsSinceTriggerReset < firearm.burstSize) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Auto) TryFire();
            }
            if (!isPulled)
            {
                shotsSinceTriggerReset = 0;
            }
        }

        public void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            ChamberSaved();
            UpdateChamberedRounds();
        }

        public override void EjectRound()
        {
            if (loadedCartridge == null) return;
            Util.PlayRandomAudioSource(ejectSounds);
            if (FirearmSaveData.GetNode(firearm).TryGetValue("ChamberSaveData", out SaveNodeValueString chamber)) chamber.value = "";
            Cartridge c = loadedCartridge;
            loadedCartridge = null;
            if (roundEjectPoint != null)
            {
                c.transform.position = roundEjectPoint.position;
                c.transform.rotation = roundEjectPoint.rotation;
            }
            Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
            c.ToggleCollision(true);
            Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
            Rigidbody rb = c.GetComponent<Rigidbody>();
            c.item.disallowDespawn = false;
            c.transform.parent = null;
            rb.isKinematic = false;
            c.loaded = false;
            rb.WakeUp();
            if (roundEjectDir != null) rb.AddForce(roundEjectDir.forward * roundEjectForce, ForceMode.Impulse);
            c.ToggleHandles(true);
            InvokeEjectRound(c);
        }

        private void Update()
        {
            BaseUpdate();
        }
    }
}