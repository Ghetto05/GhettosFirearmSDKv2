using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class SelfLoadingBolt : BoltBase
    {
        public Transform roundMount;
        public AudioSource[] ejectSounds;

        public Transform roundEjectPoint;
        public float roundEjectForce;
        public Transform roundEjectDir;
        int shotsSinceTriggerReset = 0;
        private float lastFireTime = -100f;

        public bool ReadToFire()
        {
            float timePerRound = 60f / firearm.roundsPerMinute;
            float passedTime = Time.time - lastFireTime;
            return passedTime >= timePerRound;
        }

        public override bool LoadChamber(Cartridge c, bool forced = false)
        {
            return false;
        }

        public override void TryFire()
        {
            if (firearm.magazineWell.IsEmpty()) return;
            shotsSinceTriggerReset++;
            lastFireTime = Time.time;
            foreach (RagdollHand hand in firearm.item.handlers)
            {
                if (hand.playerHand == null || hand.playerHand.controlHand == null) return;
                hand.playerHand.controlHand.HapticShort(50f);
            }
            Cartridge loadedCartridge = firearm.magazineWell.ConsumeRound();
            if (loadedCartridge.additionalMuzzleFlash != null)
            {
                loadedCartridge.additionalMuzzleFlash.transform.position = firearm.hitscanMuzzle.position;
                loadedCartridge.additionalMuzzleFlash.transform.rotation = firearm.hitscanMuzzle.rotation;
                loadedCartridge.additionalMuzzleFlash.transform.SetParent(firearm.hitscanMuzzle);
                StartCoroutine(Explosives.Explosive.delayedDestroy(loadedCartridge.additionalMuzzleFlash.gameObject, loadedCartridge.additionalMuzzleFlash.main.duration));
            }
            firearm.PlayFireSound();
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash) firearm.PlayMuzzleFlash();
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, firearm.CalculateDamageMultiplier());
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle);
            InvokeFireEvent();
        }

        public override Cartridge GetChamber()
        {
            return null;
        }

        private void UpdateChamberedRound()
        {
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (fireOnTriggerPress && isPulled && firearm.fireMode != FirearmBase.FireModes.Safe)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0 && ReadToFire()) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Burst && shotsSinceTriggerReset < firearm.burstSize && ReadToFire()) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Auto) TryFire();
            }
            if (!isPulled)
            {
                shotsSinceTriggerReset = 0;
            }
        }

        public void Awake()
        {
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            StartCoroutine(delayedGetChamber());
        }

        private void EjectRound(Cartridge c)
        {
            if (c == null) return;
            Util.PlayRandomAudioSource(ejectSounds);
            firearm.item.RemoveCustomData<ChamberSaveData>();
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
        }
    }
}