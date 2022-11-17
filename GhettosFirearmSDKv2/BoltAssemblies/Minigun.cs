using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Bolt assemblies/Minigun")]
    public class Minigun : BoltBase
    {
        public float[] barrelAngles;
        float lastRotation;
        public float rotationsPerSecond;
        public Transform roundMount;
        public Cartridge loadedCartridge;
        public Transform roundEjectPoint;
        public Transform roundEjectDir;
        public float roundEjectForce;
        public Transform barrel;

        public AudioSource RevUpSound;
        public AudioSource RevDownSound;
        public AudioSource RotatingLoop;
        public AudioSource RotatingLoopPlusFiring;
        bool trigger = false;
        bool revving = false;
        float degreesPerSecond;

        private void Awake()
        {
            firearm.item.OnHeldActionEvent += Item_OnHeldActionEvent;
            degreesPerSecond = rotationsPerSecond * 360;
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle == firearm.item.mainHandleRight)
            {
                if (action == Interactable.Action.UseStart) trigger = true;
                else if (action == Interactable.Action.UseStop) trigger = false;
                else if (action == Interactable.Action.AlternateUseStart) StartRevving();
                else if (action == Interactable.Action.AlternateUseStop) StopRevving();
            }
        }

        private void StartRevving()
        {
            if (revving) return;
            revving = true;
            RevUpSound.Play();
            RotatingLoop.PlayDelayed(RevUpSound.clip.length);
        }

        private void StopRevving()
        {
            if (!revving) return;
            revving = false;
            RotatingLoop.Stop();
            RotatingLoopPlusFiring.Stop();
            RevUpSound.Stop();
            RevDownSound.Play();
        }

        private void FixedUpdate()
        {
            if (loadedCartridge == null) TryLoadRound();
            if (revving)
            {
                barrel.Rotate(new Vector3(0, 0, degreesPerSecond * Time.deltaTime));
            }
            foreach (float degree in barrelAngles)
            {
                if (lastRotation < degree && barrel.localEulerAngles.z >= degree && trigger) TryFire();
            }
            lastRotation = barrel.localEulerAngles.z;
            if (!RotatingLoopPlusFiring.isPlaying && revving && trigger && !firearm.magazineWell.IsEmpty())
            {
                RotatingLoopPlusFiring.Play();
                RotatingLoop.Stop();
            }
            if (!RotatingLoop.isPlaying && revving && (!trigger || firearm.magazineWell.IsEmpty()))
            {
                RotatingLoop.Play();
                RotatingLoopPlusFiring.Stop();
            }
        }

        public override void TryFire()
        {
            if (loadedCartridge == null || loadedCartridge.fired) return;
            foreach (RagdollHand hand in firearm.gameObject.GetComponent<Item>().handlers)
            {
                if (hand.playerHand == null || hand.playerHand.controlHand == null) return;
                hand.playerHand.controlHand.HapticShort(50f);
            }
            if (loadedCartridge.additionalMuzzleFlash != null)
            {
                loadedCartridge.additionalMuzzleFlash.transform.position = firearm.hitscanMuzzle.position;
                loadedCartridge.additionalMuzzleFlash.transform.rotation = firearm.hitscanMuzzle.rotation;
                loadedCartridge.additionalMuzzleFlash.transform.SetParent(null);
                StartCoroutine(Explosives.Explosive.delayedDestroy(loadedCartridge.additionalMuzzleFlash.gameObject, loadedCartridge.additionalMuzzleFlash.main.duration));
            }
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash) firearm.PlayMuzzleFlash();
            firearm.PlayFireSound();
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.rb, firearm.recoilModifier, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier);
            Util.PlayRandomAudioSource(firearm.fireSounds);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories);
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle);
            EjectRound();
        }

        private void EjectRound()
        {
            if (loadedCartridge == null) return;
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
            c.item.disallowRoomDespawn = false;
            c.transform.parent = null;
            rb.isKinematic = false;
            rb.WakeUp();
            if (roundEjectDir != null) rb.AddForce(roundEjectDir.forward * roundEjectForce, ForceMode.Impulse);
            c.ToggleHandles(true);
        }

        private void TryLoadRound()
        {
            if (loadedCartridge == null && firearm.magazineWell.ConsumeRound() is Cartridge c)
            {
                loadedCartridge = c;
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = roundMount;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Vector3.zero;
            }
        }
    }
}
