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

        Cartridge loadedCartridge;

        public override void TryFire()
        {
            if (firearm.magazineWell.IsEmpty())
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            shotsSinceTriggerReset++;
            lastFireTime = Time.time;
            foreach (RagdollHand hand in firearm.item.handlers)
            {
                if (hand.playerHand != null && hand.playerHand.controlHand != null)
                    hand.playerHand.controlHand.HapticShort(50f);
            }
            loadedCartridge = firearm.magazineWell.ConsumeRound();
            if (loadedCartridge.additionalMuzzleFlash != null)
            {
                loadedCartridge.additionalMuzzleFlash.transform.position = firearm.actualHitscanMuzzle.position;
                loadedCartridge.additionalMuzzleFlash.transform.rotation = firearm.actualHitscanMuzzle.rotation;
                loadedCartridge.additionalMuzzleFlash.transform.SetParent(firearm.actualHitscanMuzzle);
                loadedCartridge.additionalMuzzleFlash.Play();
                StartCoroutine(Explosives.Explosive.delayedDestroy(loadedCartridge.additionalMuzzleFlash.gameObject, loadedCartridge.additionalMuzzleFlash.main.duration));
            }
            firearm.PlayFireSound(loadedCartridge);
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                firearm.PlayMuzzleFlash(loadedCartridge);
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, true);
            EjectRound();
            InvokeFireEvent();
            InvokeFireLogicFinishedEvent();
        }

        public override Cartridge GetChamber()
        {
            return null;
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
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

        public void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            ChamberSaved();
        }

        public override void EjectRound()
        {
            if (loadedCartridge == null) return;
            Util.PlayRandomAudioSource(ejectSounds);
            if (FirearmSaveData.GetNode(firearm).TryGetValue("ChamberSaveData", out SaveNodeValueString chamber)) chamber.value = "";
            if (roundEjectPoint != null)
            {
                loadedCartridge.transform.position = roundEjectPoint.position;
                loadedCartridge.transform.rotation = roundEjectPoint.rotation;
            }
            Util.IgnoreCollision(loadedCartridge.gameObject, firearm.gameObject, true);
            loadedCartridge.ToggleCollision(true);
            Util.DelayIgnoreCollision(loadedCartridge.gameObject, firearm.gameObject, false, 3f, firearm.item);
            Rigidbody rb = loadedCartridge.GetComponent<Rigidbody>();
            loadedCartridge.item.disallowDespawn = false;
            loadedCartridge.transform.parent = null;
            rb.isKinematic = false;
            loadedCartridge.loaded = false;
            rb.WakeUp();
            if (roundEjectDir != null) rb.AddForce(roundEjectDir.forward * roundEjectForce, ForceMode.Impulse);
            loadedCartridge.ToggleHandles(true);
            if (firearm.magazineWell != null && firearm.magazineWell.IsEmptyAndHasMagazine() && firearm.magazineWell.currentMagazine.ejectOnLastRoundFired) firearm.magazineWell.Eject();
        }
    }
}